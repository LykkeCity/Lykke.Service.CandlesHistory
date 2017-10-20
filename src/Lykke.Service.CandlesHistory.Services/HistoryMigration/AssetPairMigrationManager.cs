using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
using Lykke.Service.CandlesHistory.Core.Extensions;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    public class AssetPairMigrationManager
    {
        private readonly AssetPairMigrationHealthService _healthService;
        private readonly string _assetPair;
        private readonly ILog _log;
        private readonly CandleMigrationService _candleMigrationService;
        private readonly ICandlesManager _candlesManager;
        private readonly Action<string> _onStoppedAction;
        private readonly CancellationTokenSource _cts;
        private readonly MigrationCandlesGenerator _candlesGenerator;

        private readonly TimeInterval[] _intervalsToGenerate =
        {
            TimeInterval.Minute,
            TimeInterval.Hour,
            TimeInterval.Day,
            TimeInterval.Week,
            TimeInterval.Month
        };

        public AssetPairMigrationManager(
            MigrationCandlesGenerator candlesGenerator,
            AssetPairMigrationHealthService healthService,
            string assetPair,
            ILog log,
            CandleMigrationService candleMigrationService,
            ICandlesManager candlesManager,
            Action<string> onStoppedAction)
        {
            _candlesGenerator = candlesGenerator;
            _healthService = healthService;
            _assetPair = assetPair;
            _log = log;
            _candleMigrationService = candleMigrationService;
            _candlesManager = candlesManager;
            _onStoppedAction = onStoppedAction;

            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            Task.Run(() => MigrateAsync().Wait());
        }

        private async Task MigrateAsync()
        {
            try
            {
                _healthService.UpdateOverallProgress("Obtaining bid and ask start dates");

                var startDates = await Task.WhenAll(
                    _candleMigrationService.GetStartDateAsync(_assetPair, PriceType.Ask),
                    _candleMigrationService.GetStartDateAsync(_assetPair, PriceType.Bid));
                var askStartDate = startDates[0];
                var bidStartDate = startDates[1];

                _healthService.UpdateStartDates(askStartDate, bidStartDate);
                _healthService.UpdateOverallProgress("Obtaining bid and ask end dates");

                var endDates = await Task.WhenAll(
                    _candleMigrationService.GetEndDateAsync(_assetPair, PriceType.Ask),
                    _candleMigrationService.GetEndDateAsync(_assetPair, PriceType.Bid));
                var askEndDate = endDates[0];
                var bidEndDate = endDates[1];

                _healthService.UpdateEndDates(askEndDate, bidEndDate);
                _healthService.UpdateOverallProgress("Processing bid and ask feed history");

                var processAskCandlesTask = _candleMigrationService.GetCandlesByChunkAsync(_assetPair, PriceType.Ask,
                    askStartDate, askEndDate, ProcessFeedHistoryChunkAsync);
                var processBidkCandlesTask = _candleMigrationService.GetCandlesByChunkAsync(_assetPair, PriceType.Bid,
                    bidStartDate, bidEndDate, ProcessFeedHistoryChunkAsync);

                await Task.WhenAll(processAskCandlesTask, processBidkCandlesTask);

                var startDateMid = askStartDate < bidStartDate ? askStartDate : bidStartDate;
                var endDateMid = askEndDate > bidEndDate ? askEndDate : bidEndDate;

                _healthService.UpdateOverallProgress("Generating mid history");

                await _candleMigrationService.GetHistoryBidAskByChunkAsync(_assetPair, startDateMid, endDateMid,
                    ProcessBidAskHistoryChunkAsync);

                await _candleMigrationService.RemoveProcessedDateAsync(_assetPair);

                _healthService.UpdateOverallProgress("Done");
            }
            catch (Exception ex)
            {
                _healthService.UpdateOverallProgress($"Failed: {ex}");
                
                await _log.WriteErrorAsync(nameof(AssetPairMigrationManager), nameof(MigrateAsync), _assetPair, ex);
            }
            finally
            {
                try
                {
                    _onStoppedAction.Invoke(_assetPair);
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(nameof(AssetPairMigrationManager), nameof(MigrateAsync), _assetPair, ex);
                }
            }
        }

        private async Task ProcessFeedHistoryChunkAsync(IEnumerable<IFeedHistory> items, PriceType priceType)
        {
            foreach (var feedHistory in items)
            {
                _healthService.UpdateCurrentHistoryDate(feedHistory.DateTime, priceType);
                
                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                var secCandles = _candleMigrationService.GenerateMissedCandles(feedHistory);

                if (await ProcessSecCandles(secCandles))
                {
                    return;
                }

                await _candleMigrationService.SaveBidAskHistoryAsync(_assetPair, feedHistory.DateTime, 
                    priceType == PriceType.Ask ? secCandles : null,
                    priceType == PriceType.Bid ? secCandles : null);
                await _candleMigrationService.SetProcessedDateAsync(feedHistory.AssetPair, priceType, feedHistory.DateTime);
            }
        }

        private async Task ProcessBidAskHistoryChunkAsync(IEnumerable<IFeedBidAskHistory> items)
        {
            foreach (var feedHistory in items)
            {
                _healthService.UpdateCurrentHistoryDate(feedHistory.DateTime, PriceType.Mid);

                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                if (feedHistory.AskCandles.Any() && feedHistory.BidCandles.Any())
                {
                    var midSecCandles = feedHistory.AskCandles.CreateMidCandles(feedHistory.BidCandles);

                    if (!(await ProcessSecCandles(midSecCandles)))
                    {
                        return;
                    }
                }

                await _candleMigrationService.SetProcessedDateAsync(feedHistory.AssetPair, PriceType.Mid, feedHistory.DateTime);
            }
        }

        private async Task<bool> ProcessSecCandles(IEnumerable<ICandle> secCandles)
        {
            foreach (var candle in secCandles)
            {
                foreach (var interval in _intervalsToGenerate)
                {
                    if (_cts.IsCancellationRequested)
                    {
                        return true;
                    }

                    var result = _candlesGenerator.Merge(candle, interval);

                    if (result.WasChanged)
                        await _candlesManager.ProcessCandleAsync(result.Candle);
                }

                await _candlesManager.ProcessCandleAsync(candle);
            }

            return false;
        }

        public void Stop()
        {
            _cts.Cancel();
        }
    }
}
