using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Extensions;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class AssetPairMigrationManager
    {
        private readonly string _assetPair;
        private readonly ILog _log;
        private readonly CandleMigrationService _candleMigrationService;
        private readonly ICandlesManager _candlesManager;
        private readonly Action<string> _onCompleteAction;
        private readonly CancellationTokenSource _cts;
        private readonly CandlesGenerator _candlesGenerator;

        private readonly TimeInterval[] _intervalsToGenerate = {TimeInterval.Minute, TimeInterval.Hour, TimeInterval.Day, TimeInterval.Week, TimeInterval.Month};

        public AssetPairMigrationManager(
            string assetPair, 
            ILog log,
            CandleMigrationService candleMigrationService,
            ICandlesManager candlesManager,
            Action<string> onCompleteAction)
        {
            _assetPair = assetPair;
            _log = log;
            _candleMigrationService = candleMigrationService;
            _candlesManager = candlesManager;
            _onCompleteAction = onCompleteAction;
            _cts = new CancellationTokenSource();
            _candlesGenerator = new CandlesGenerator();
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                try
                {
                    var startDateAsk = await _candleMigrationService.GetStartDateAsync(_assetPair, PriceType.Ask);
                    var startDateBid = await _candleMigrationService.GetStartDateAsync(_assetPair, PriceType.Bid);
                    var endDate = await _candleMigrationService.GetEndDateAsync(_assetPair);

                    var processAskCandlesTask = _candleMigrationService.GetCandlesByChunkAsync(_assetPair, PriceType.Ask, startDateAsk, endDate, ProcessFeedHistory);
                    var processBidkCandlesTask = _candleMigrationService.GetCandlesByChunkAsync(_assetPair, PriceType.Bid, startDateBid, endDate, ProcessFeedHistory);

                    Task.WaitAll(processAskCandlesTask, processBidkCandlesTask);

                    //process mid candles
                    await _candleMigrationService.GetHistoryBidAskByChunkAsync(_assetPair, startDateAsk < startDateBid ? startDateAsk : startDateBid, endDate, ProcessBidAskHistory);
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(nameof(AssetPairMigrationManager), nameof(Start), _assetPair, ex);
                }
                finally
                {
                    try
                    {
                        _onCompleteAction.Invoke(_assetPair);
                    }
                    catch (Exception ex)
                    {
                        await _log.WriteErrorAsync(nameof(AssetPairMigrationManager), nameof(Start), _assetPair, ex);
                    }
                }
            }, _cts.Token);
        }

        private async Task ProcessFeedHistory(IEnumerable<IFeedHistory> items, PriceType priceType)
        {
            foreach (var feedHistory in items)
            {
                var secCandles = _candleMigrationService.GenerateMissedCandles(feedHistory, TimeInterval.Sec);

                foreach (var candle in secCandles)
                {
                    foreach (var interval in _intervalsToGenerate)
                    {
                        var result = _candlesGenerator.Merge(candle, interval, candle.PriceType.ToString());

                        if (result.WasChanged)
                            await _candlesManager.ProcessCandleAsync(result.Candle);
                    }

                    await _candlesManager.ProcessCandleAsync(candle);
                }

                await _candleMigrationService.AddHistoryBidAskByChunkAsync(_assetPair, feedHistory.DateTime, priceType == PriceType.Ask ? secCandles : null,
                    priceType == PriceType.Bid ? secCandles : null);
                await _candleMigrationService.SetProcessedCandleAsync(feedHistory.AssetPair, priceType, feedHistory.DateTime);
            }
        }

        private async Task ProcessBidAskHistory(IEnumerable<IFeedBidAskHistory> items)
        {
            foreach (var feedHistory in items)
            {
                if (feedHistory.AskCandles.Any() && feedHistory.BidCandles.Any())
                {
                    var midSecCandles = feedHistory.AskCandles.CreateMidCandles(feedHistory.BidCandles);

                    foreach (var candle in midSecCandles)
                    {
                        foreach (var interval in _intervalsToGenerate)
                        {
                            var result = _candlesGenerator.Merge(candle, interval, "mid");

                            if (result.WasChanged)
                                await _candlesManager.ProcessCandleAsync(result.Candle);
                        }

                        await _candlesManager.ProcessCandleAsync(candle);
                    }
                }
            }
        }

        public void Stop()
        {
            _cts.Cancel();
        }
    }
}
