using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Extensions;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration.HistoryProviders;
using Lykke.Service.CandlesHistory.Services.Candles;
using Lykke.Service.CandlesHistory.Services.HistoryMigration.Telemetry;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    public class AssetPairMigrationManager
    {
        private readonly AssetPairMigrationTelemetryService _telemetryService;
        private readonly IAssetPair _assetPair;
        private readonly ILog _log;
        private readonly BidAskHCacheService _bidAskHCacheService;
        private readonly IHistoryProvider _historyProvider;
        private readonly ICandlesHistoryMigrationService _candlesHistoryMigrationService;
        private readonly Action<string> _onStoppedAction;
        private readonly CancellationTokenSource _cts;
        private readonly ICandlesPersistenceQueue _candlesPersistenceQueue;
        private readonly MigrationCandlesGenerator _candlesGenerator;
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
            AssetPairMigrationTelemetryService telemetryService,
            IAssetPair assetPair,
            ILog log,
            BidAskHCacheService bidAskHCacheService,
            IHistoryProvider historyProvider,
            ICandlesHistoryMigrationService candlesHistoryMigrationService,
            Action<string> onStoppedAction)
        {
            _candlesPersistenceQueue = candlesPersistenceQueue;
            _candlesGenerator = candlesGenerator;
            _telemetryService = telemetryService;
            _assetPair = assetPair;
            _log = log;
            _bidAskHCacheService = bidAskHCacheService;
            _historyProvider = historyProvider;
            _candlesHistoryMigrationService = candlesHistoryMigrationService;
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
                _telemetryService.UpdateOverallProgress("Obtaining bid and ask start dates");

                (DateTime? askStartDate, DateTime? bidStartDate) = await GetStartDatesAsync();

                _prevAskTimestamp = askStartDate?.AddSeconds(-1) ?? DateTime.MinValue;
                _prevBidTimestamp = bidStartDate?.AddSeconds(-1) ?? DateTime.MinValue;
                
                _telemetryService.UpdateStartDates(askStartDate, bidStartDate);
                _telemetryService.UpdateOverallProgress("Obtaining bid and ask end dates");

                var now = DateTime.UtcNow.RoundToSecond();

                (ICandle askEndCandle, ICandle bidEndCandle) = await GetFirstTargetHistoryCandlesAsync(askStartDate, bidStartDate);

                var askEndDate = askEndCandle?.Timestamp ?? now;
                var bidEndDate = bidEndCandle?.Timestamp ?? now;

                _telemetryService.UpdateEndDates(askEndDate, bidEndDate);
                _telemetryService.UpdateOverallProgress("Processing bid and ask feed history, generating mid history");

                await Task.WhenAll(
                    ProcessAskAndBidHistoryAsync(askStartDate, askEndDate, askEndCandle, bidStartDate, bidEndDate, bidEndCandle),
                    askStartDate.HasValue && bidStartDate.HasValue
                        ? GenerateMidHistoryAsync(askStartDate.Value, bidStartDate.Value, askEndDate, bidEndDate)
                        : Task.CompletedTask);

                _telemetryService.UpdateOverallProgress("Done");
            }
            catch (Exception ex)
            {
                _telemetryService.UpdateOverallProgress($"Failed: {ex}");
                
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
                _historyProvider.GetStartDateAsync(_assetPair.Id, PriceType.Ask),
                _historyProvider.GetStartDateAsync(_assetPair.Id, PriceType.Bid));

            return (askStartDate: startDates[0], bidStartDate: startDates[1]);
        }

        private async Task<(ICandle askCandle, ICandle bidCandle)> GetFirstTargetHistoryCandlesAsync(DateTime? askStartDate, DateTime? bidStartDate)
        {
            var getAskEndCandleTask = askStartDate.HasValue
                ? _candlesHistoryMigrationService.GetFirstCandleOfHistoryAsync(_assetPair.Id, PriceType.Ask)
                : Task.FromResult<ICandle>(null);
            var getBidEndCandleTask = bidStartDate.HasValue
                ? _candlesHistoryMigrationService.GetFirstCandleOfHistoryAsync(_assetPair.Id, PriceType.Bid)
                : Task.FromResult<ICandle>(null);
            var endCandles = await Task.WhenAll(getAskEndCandleTask, getBidEndCandleTask);

            return (askCandle: endCandles[0], bidCandle: endCandles[1]);
        }

        private async Task ProcessAskAndBidHistoryAsync(
            DateTime? askStartDate, DateTime askEndDate, ICandle askEndCandle,
            DateTime? bidStartDate, DateTime bidEndDate, ICandle bidEndCandle)
        {
            try
            {
                var processAskCandlesTask = askStartDate.HasValue
                    ? _historyProvider.GetHistoryByChunksAsync(_assetPair, PriceType.Ask, askEndDate, askEndCandle, ProcessHistoryChunkAsync, _cts.Token)
                    : Task.CompletedTask;
                var processBidkCandlesTask = bidStartDate.HasValue
                    ? _historyProvider.GetHistoryByChunksAsync(_assetPair, PriceType.Bid, bidEndDate, bidEndCandle, ProcessHistoryChunkAsync, _cts.Token)
                    : Task.CompletedTask;

                await Task.WhenAll(processAskCandlesTask, processBidkCandlesTask);
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

                while (_telemetryService.CurrentMidDate < midEndTime && !_cts.IsCancellationRequested)
                {
                    // Lets migrate some bid and ask history
                    await Task.Delay(1000);

                    var bidAskHistory = _bidAskHCacheService.PopReadyHistory(midStartTime);
                    var secMidCandles = new List<ICandle>();

                    foreach (var item in bidAskHistory)
                    {
                        _telemetryService.UpdateCurrentHistoryDate(item.timestamp, PriceType.Mid);

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
                    }

                    if (ProcessSecCandles(secMidCandles))
                    {
                        return;
                    }
                }
            }
            catch
            {
                _cts.Cancel();
                throw;
            }
        }

        private async Task ProcessHistoryChunkAsync(IReadOnlyList<ICandle> candles)
        {
            try
            {
                ProcessSecCandles(candles);

                _bidAskHCacheService.PushHistory(candles);

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
                _telemetryService.UpdateCurrentHistoryDate(candle.Timestamp, candle.PriceType);
                
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
                //if (distance > TimeSpan.FromSeconds(1))
                //{
                //    throw new InvalidOperationException($"Candle {priceType} timestamp is skipped at {currentTimestamp}, prev was {prevTimestamp}");
                //}

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
