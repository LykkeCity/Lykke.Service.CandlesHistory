using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
using Lykke.Service.CandlesHistory.Core.Extensions;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration;
using Lykke.Service.CandlesHistory.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    public class AssetPairMigrationManager
    {
        private readonly AssetPairMigrationHealthService _healthService;
        private readonly IAssetPair _assetPair;
        private readonly ILog _log;
        private readonly ICandlesMigrationService _candlesMigrationService;
        private readonly BidAskHistoryService _bidAskHistoryService;
        private readonly Action<string> _onStoppedAction;
        private readonly CancellationTokenSource _cts;
        private readonly ICandlesPersistenceQueue _candlesPersistenceQueue;
        private readonly MigrationCandlesGenerator _candlesGenerator;
        private readonly MissedCandlesGenerator _missedCandlesGenerator;

        private readonly ImmutableArray<TimeInterval> _intervalsToGenerate = Constants
            .StoredIntervals
            .Where(i => i != TimeInterval.Sec)
            .ToImmutableArray();

        public AssetPairMigrationManager(
            ICandlesPersistenceQueue candlesPersistenceQueue,
            MigrationCandlesGenerator candlesGenerator,
            MissedCandlesGenerator missedCandlesGenerator,
            AssetPairMigrationHealthService healthService,
            IAssetPair assetPair,
            ILog log,
            ICandlesMigrationService candlesMigrationService,
            BidAskHistoryService bidAskHistoryService,
            Action<string> onStoppedAction)
        {
            _candlesPersistenceQueue = candlesPersistenceQueue;
            _candlesGenerator = candlesGenerator;
            _missedCandlesGenerator = missedCandlesGenerator;
            _healthService = healthService;
            _assetPair = assetPair;
            _log = log;
            _candlesMigrationService = candlesMigrationService;
            _bidAskHistoryService = bidAskHistoryService;
            _onStoppedAction = onStoppedAction;

            _cts = new CancellationTokenSource();
        }

        public void StartRandom(DateTime start, DateTime end, double startPrice, double endPrice, double spread)
        {
            Task.Run(() => RandomizeAsync(start, end, startPrice, endPrice, spread).Wait());
        }

        public void Start()
        {
            Task.Run(() => MigrateAsync().Wait());
        }

        private async Task RandomizeAsync(DateTime start, DateTime end, double startPrice, double endPrice, double spread)
        {
            try
            {
                _healthService.UpdateOverallProgress($"Randomizing bid and ask candles in the dates range {start} - {end} and prices {startPrice} - {endPrice} and generating mid history");
                _healthService.UpdateStartDates(start, start);
                _healthService.UpdateEndDates(end, end);

                await Task.WhenAll(
                    RandomizeCandlesAsync(PriceType.Ask, start, end, startPrice, endPrice, spread),
                    RandomizeCandlesAsync(PriceType.Bid, start, end, startPrice, endPrice, spread),
                    GenerateMidHistoryAsync(end, end));

                //await _candlesMigrationService.RemoveProcessedDateAsync(_assetPair.Id);

                _healthService.UpdateOverallProgress("Done");
            }
            catch (Exception ex)
            {
                _healthService.UpdateOverallProgress($"Failed: {ex}");

                await _log.WriteErrorAsync(nameof(AssetPairMigrationManager), nameof(MigrateAsync), _assetPair.Id, ex);
            }
            finally
            {
                try
                {
                    _onStoppedAction.Invoke(_assetPair.Id);
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(nameof(AssetPairMigrationManager), nameof(MigrateAsync), _assetPair.Id, ex);
                }
            }
        }

        private async Task RandomizeCandlesAsync(PriceType priceType, DateTime start, DateTime end, double startPrice, double endPrice, double spread)
        {
            var secCandles = _missedCandlesGenerator
                .GenerateCandles(_assetPair, priceType, start, end, startPrice, endPrice, spread)
                .ToArray();

            if (ProcessSecCandles(secCandles))
            {
                return;
            }

            _bidAskHistoryService.PushHistory(secCandles);

            await Task.CompletedTask;
        }

        private async Task MigrateAsync()
        {
            try
            {
                _healthService.UpdateOverallProgress("Obtaining bid and ask start dates");

                (DateTime? askStartDate, DateTime? bidStartDate) = await GetStartDatesAsync();

                _healthService.UpdateStartDates(askStartDate, bidStartDate);
                _healthService.UpdateOverallProgress("Obtaining bid and ask end dates");

                (DateTime askEndDate, DateTime bidEndDate) = await GetEndDatesAsync(askStartDate, bidStartDate);

                _healthService.UpdateEndDates(askEndDate, bidEndDate);
                _healthService.UpdateOverallProgress("Processing bid and ask feed history, generating mid history");

                await Task.WhenAll(
                    ProcessAskAndBidHistoryAsync(askStartDate, askEndDate, bidStartDate, bidEndDate),
                    askStartDate.HasValue && bidStartDate.HasValue
                        ? GenerateMidHistoryAsync(askEndDate, bidEndDate)
                        : Task.CompletedTask);

                //await _candlesMigrationService.RemoveProcessedDateAsync(_assetPair.Id);

                _healthService.UpdateOverallProgress("Done");
            }
            catch (Exception ex)
            {
                _healthService.UpdateOverallProgress($"Failed: {ex}");
                
                await _log.WriteErrorAsync(nameof(AssetPairMigrationManager), nameof(MigrateAsync), _assetPair.Id, ex);
            }
            finally
            {
                try
                {
                    _onStoppedAction.Invoke(_assetPair.Id);
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(nameof(AssetPairMigrationManager), nameof(MigrateAsync), _assetPair.Id, ex);
                }
            }
        }

        private async Task<(DateTime? askStartDate, DateTime? bidStartDate)> GetStartDatesAsync()
        {
            var startDates = await Task.WhenAll(
                _candlesMigrationService.GetStartDateAsync(_assetPair.Id, PriceType.Ask),
                _candlesMigrationService.GetStartDateAsync(_assetPair.Id, PriceType.Bid));

            return (askStartDate: startDates[0], bidStartDate: startDates[1]);
        }

        private async Task<(DateTime askEndDate, DateTime bidEndDate)> GetEndDatesAsync(DateTime? askStartDate, DateTime? bidStartDate)
        {
            var now = DateTime.UtcNow;
            var getAskEndDateTask = askStartDate.HasValue
                ? _candlesMigrationService.GetEndDateAsync(_assetPair.Id, PriceType.Ask, now)
                : Task.FromResult(DateTime.MaxValue);
            var getBidEndDateTask = bidStartDate.HasValue
                ? _candlesMigrationService.GetEndDateAsync(_assetPair.Id, PriceType.Bid, now)
                : Task.FromResult(DateTime.MaxValue);
            var endDates = await Task.WhenAll(getAskEndDateTask, getBidEndDateTask);

            return (askEndDate: endDates[0], bidEndDate: endDates[1]);
        }

        private async Task ProcessAskAndBidHistoryAsync(
            DateTime? askStartDate, DateTime askEndDate, 
            DateTime? bidStartDate, DateTime bidEndDate)
        {
            var processAskCandlesTask = askStartDate.HasValue
                ? _candlesMigrationService.GetFeedHistoryCandlesByChunkAsync(_assetPair.Id, PriceType.Ask, askStartDate.Value,
                    askEndDate, ProcessFeedHistoryChunkAsync)
                : Task.CompletedTask;
            var processBidkCandlesTask = bidStartDate.HasValue
                ? _candlesMigrationService.GetFeedHistoryCandlesByChunkAsync(_assetPair.Id, PriceType.Bid, bidStartDate.Value,
                    bidEndDate, ProcessFeedHistoryChunkAsync)
                : Task.CompletedTask;

            await Task.WhenAll(processAskCandlesTask, processBidkCandlesTask);

            _healthService.UpdateOverallProgress("Generate ending of missed feed history");
            
            await Task.WhenAll(
                GenerateAskBidMissedCandlesEndingAsync(PriceType.Ask, askEndDate),
                GenerateAskBidMissedCandlesEndingAsync(PriceType.Bid, bidEndDate));
        }

        private async Task GenerateAskBidMissedCandlesEndingAsync(PriceType priceType, DateTime endDateTime)
        {
            var secCandles = _missedCandlesGenerator.FillGapUpTo(_assetPair, priceType, endDateTime);

            if (ProcessSecCandles(secCandles))
            {
                return;
            }

            _bidAskHistoryService.PushHistory(secCandles);
            //await _candlesMigrationService.SetProcessedDateAsync(_assetPair.Id, priceType, endDateTime);

            await Task.CompletedTask;
        }

        private async Task GenerateMidHistoryAsync(DateTime askEndTime, DateTime bidEndTime)
        {
            var midEndTime = (askEndTime < bidEndTime ? askEndTime : bidEndTime).AddSeconds(-1);
            
            while (_healthService.CurrentMidDate < midEndTime)
            {
                // Lets migrate some bid and ask history
                await Task.Delay(1000);

                var bidAskHistory = _bidAskHistoryService.PopReadyHistory();
                var secMidCandles = new List<ICandle>();

                foreach (var item in bidAskHistory)
                {
                    _healthService.UpdateCurrentHistoryDate(item.timestamp, PriceType.Mid);

                    if (_cts.IsCancellationRequested)
                    {
                        return;
                    }

                    if (item.ask == null && item.bid == null)
                    {
                        await _log.WriteWarningAsync(nameof(AssetPairMigrationManager), nameof(GenerateMidHistoryAsync),
                            $"{_assetPair}-{item.timestamp}", "bid or ask candle is empty");
                        continue;
                    }

                    var midSecCandle = item.ask.CreateMidCandle(item.bid);

                    secMidCandles.Add(midSecCandle);

                    //await _candlesMigrationService.SetProcessedDateAsync(feedHistory.AssetPair, PriceType.Mid, feedHistory.DateTime);
                }

                ProcessSecCandles(secMidCandles);
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

                var secCandles = _missedCandlesGenerator.FillGapUpTo(_assetPair, feedHistory);

                if (ProcessSecCandles(secCandles))
                {
                    return;
                }

                _bidAskHistoryService.PushHistory(secCandles);
                //await _candlesMigrationService.SetProcessedDateAsync(feedHistory.AssetPair, priceType, feedHistory.DateTime);
            }

            await Task.CompletedTask;
        }

        private bool ProcessSecCandles(IEnumerable<ICandle> secCandles)
        {
            var changedCandles = new Dictionary<(TimeInterval interval, DateTime timestamp), ICandle>();

            foreach (var candle in secCandles)
            {
                foreach (var interval in _intervalsToGenerate)
                {
                    if (_cts.IsCancellationRequested)
                    {
                        return true;
                    }

                    var mergingResult = _candlesGenerator.Merge(candle, interval);

                    if (mergingResult.WasChanged)
                    {
                        changedCandles[(interval, mergingResult.Candle.Timestamp)] = mergingResult.Candle;
                    }
                }

                _candlesPersistenceQueue.EnqueueCandle(candle);
            }

            foreach (var changedCandle in changedCandles.Values)
            {
                _candlesPersistenceQueue.EnqueueCandle(changedCandle);
            }

            return false;
        }

        public void Stop()
        {
            _cts.Cancel();
        }
    }
}
