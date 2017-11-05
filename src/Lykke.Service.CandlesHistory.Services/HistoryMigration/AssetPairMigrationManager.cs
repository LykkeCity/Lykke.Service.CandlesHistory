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
        private DateTime _prevAskTimestamp;
        private DateTime _prevBidTimestamp;
        private DateTime _prevMidTimestamp;

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

        public void Start()
        {
            Task.Run(() => MigrateAsync().Wait());
        }

        private async Task MigrateAsync()
        {
            try
            {
                _healthService.UpdateOverallProgress("Obtaining bid and ask start dates");

                (DateTime? askStartDate, DateTime? bidStartDate) = await GetStartDatesAsync();

                _prevAskTimestamp = askStartDate?.AddSeconds(-1) ?? DateTime.MinValue;
                _prevBidTimestamp = bidStartDate?.AddSeconds(-1) ?? DateTime.MinValue;
                
                _healthService.UpdateStartDates(askStartDate, bidStartDate);
                _healthService.UpdateOverallProgress("Obtaining bid and ask end dates");

                (DateTime askEndDate, DateTime bidEndDate) = await GetEndDatesAsync(askStartDate, bidStartDate);

                _healthService.UpdateEndDates(askEndDate, bidEndDate);
                _healthService.UpdateOverallProgress("Processing bid and ask feed history, generating mid history");

                await Task.WhenAll(
                    ProcessAskAndBidHistoryAsync(askStartDate, askEndDate, bidStartDate, bidEndDate),
                    askStartDate.HasValue && bidStartDate.HasValue
                        ? GenerateMidHistoryAsync(askStartDate.Value, bidStartDate.Value, askEndDate, bidEndDate)
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
            try
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

                _healthService.UpdateOverallProgress($"Generate ending of missed feed history from ask {_healthService.CurrentAskDate:O}, bid {_healthService.CurrentBidDate:O}");

                await Task.WhenAll(
                    GenerateAskBidMissedCandlesEndingAsync(PriceType.Ask, askEndDate),
                    GenerateAskBidMissedCandlesEndingAsync(PriceType.Bid, bidEndDate));
            }
            catch
            {
                _cts.Cancel();
                throw;
            }
        }

        private async Task GenerateAskBidMissedCandlesEndingAsync(PriceType priceType, DateTime endDateTime)
        {
            try
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
            catch
            {
                _cts.Cancel();
                throw;
            }
        }

        private async Task GenerateMidHistoryAsync(DateTime askStartTime, DateTime bidStartTime, DateTime askEndTime, DateTime bidEndTime)
        {
            try
            {
                var midStartTime = askStartTime < bidStartTime ? bidStartTime : askStartTime;
                var midEndTime = (askEndTime < bidEndTime ? askEndTime : bidEndTime).AddSeconds(-1);

                _prevMidTimestamp = midStartTime.AddSeconds(-1);

                while (_healthService.CurrentMidDate < midEndTime && !_cts.IsCancellationRequested)
                {
                    // Lets migrate some bid and ask history
                    await Task.Delay(1000);

                    var bidAskHistory = _bidAskHistoryService.PopReadyHistory(midStartTime);
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
            catch
            {
                _cts.Cancel();
                throw;
            }
        }

        private async Task ProcessFeedHistoryChunkAsync(IEnumerable<IFeedHistory> items, PriceType priceType)
        {
            try
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
            catch
            {
                _cts.Cancel();
                throw;
            }
        }

        private bool ProcessSecCandles(IEnumerable<ICandle> secCandles)
        {
            var changedCandles = new Dictionary<(TimeInterval interval, DateTime timestamp), ICandle>();

            foreach (var candle in secCandles)
            {
                CheckCandleOrder(candle);

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

        private void CheckCandleOrder(ICandle candle)
        {
            DateTime CheckTimestamp(PriceType priceType, DateTime prevTimestamp, DateTime currentTimestamp)
            {
                var distance = currentTimestamp - prevTimestamp;

                if (distance == TimeSpan.Zero)
                {
                    throw new InvalidOperationException($"Candle {priceType} timestamp duplicated at {currentTimestamp}");
                }
                if (distance < TimeSpan.Zero)
                {
                    throw new InvalidOperationException($"Candle {priceType} timestamp is to old at {currentTimestamp}, prev was {prevTimestamp}");
                }
                if (distance > TimeSpan.FromSeconds(1))
                {
                    throw new InvalidOperationException($"Candle {priceType} timestamp is skipped at {currentTimestamp}, prev was {prevTimestamp}");
                }

                return currentTimestamp;
            }

            switch (candle.PriceType)
            {
                case PriceType.Ask:
                    _prevAskTimestamp = CheckTimestamp(PriceType.Ask, _prevAskTimestamp, candle.Timestamp);
                    break;
                case PriceType.Bid:
                    _prevBidTimestamp = CheckTimestamp(PriceType.Bid, _prevBidTimestamp, candle.Timestamp);
                    break;
                case PriceType.Mid:
                    _prevMidTimestamp = CheckTimestamp(PriceType.Mid, _prevMidTimestamp, candle.Timestamp);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(candle.PriceType), candle.PriceType, "Invalid price type");
            }
        }

        public void Stop()
        {
            _cts.Cancel();
        }
    }
}
