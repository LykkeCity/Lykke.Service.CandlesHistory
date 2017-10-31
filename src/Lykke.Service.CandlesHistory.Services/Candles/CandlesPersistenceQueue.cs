using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Settings;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesPersistenceQueue : 
        ProducerConsumer<IReadOnlyCollection<ICandle>>,
        ICandlesPersistenceQueue
    {
        private readonly ICandlesHistoryRepository _repository;
        private readonly IFailedToPersistCandlesPublisher _failedToPersistCandlesPublisher;
        private readonly ILog _log;
        private readonly IHealthService _healthService;
        private readonly PersistenceSettings _settings;

        private ConcurrentQueue<ICandle> _candlesToDispatch;
        
        public CandlesPersistenceQueue(
            ICandlesHistoryRepository repository,
            IFailedToPersistCandlesPublisher failedToPersistCandlesPublisher,
            ILog log,
            IHealthService healthService,
            PersistenceSettings settings) :

            base(nameof(CandlesPersistenceQueue), log)
        {
            _repository = repository;
            _failedToPersistCandlesPublisher = failedToPersistCandlesPublisher;
            _log = log;
            _healthService = healthService;
            _settings = settings;
            _candlesToDispatch = new ConcurrentQueue<ICandle>();
        }

        public void EnqueueCandle(ICandle candle)
        {
            if (_healthService.CandlesToDispatchQueueLength > _settings.CandlesToDispatchLengthThrottlingThreshold)
            {
                Task.Delay(_settings.ThrottlingEnqueueDelay).Wait();
            }

            _candlesToDispatch.Enqueue(candle);

            _healthService.TraceEnqueueCandle();
        }

        public IImmutableList<ICandle> GetState()
        {
            return _candlesToDispatch.ToArray().ToImmutableList();
        }

        public void SetState(IImmutableList<ICandle> state)
        {
            if (_candlesToDispatch.Count > 0)
            {
                throw new InvalidOperationException("Queue state can't be set when queue already not empty");
            }

            _candlesToDispatch = new ConcurrentQueue<ICandle>(state);

            _healthService.TraceSetPersistenceQueueState(state.Count);
        }

        public string DescribeState(IImmutableList<ICandle> state)
        {
            return $"Candles: {state.Count}";
        }

        public void DispatchCandlesToPersist(int maxBatchSize)
        {
            var candlesCount = _candlesToDispatch.Count;

            if (candlesCount == 0)
            {
                return;
            }

            candlesCount = Math.Min(candlesCount, maxBatchSize);

            var candles = new List<ICandle>(candlesCount);

            for (var i = 0; i < candlesCount; i++)
            {
                if (_candlesToDispatch.TryDequeue(out var candle))
                {
                    candles.Add(candle);
                }
                else
                {
                    break;
                }
            }

            _healthService.TraceCandlesBatchDispatched(candles.Count);

            // Add candles to producer/consumer's queue
            Produce(candles);
        }

        protected override async Task Consume(IReadOnlyCollection<ICandle> candles)
        {
#if DEBUG
            await _log.WriteInfoAsync(nameof(CandlesPersistenceQueue), nameof(Consume), "", 
                $"Consuming candles batch with {candles.Count} candles. Amount of batches in queue={_healthService.BatchesToPersistQueueLength}. Amount of candles to dispath={_healthService.CandlesToDispatchQueueLength}");
#endif
            try
            {
                await PersistCandles(candles);
            }
            finally
            {
                _healthService.TraceCandlesBatchPersisted(candles.Count);
            }
#if DEBUG
            await _log.WriteInfoAsync(nameof(CandlesPersistenceQueue), nameof(Consume), "", 
                $"Candles batch with {candles.Count} candles is persisted. Amount of batches in queue={_healthService.BatchesToPersistQueueLength}. Amount of candles to dispath={_healthService.CandlesToDispatchQueueLength}");
#endif
        }

        private async Task PersistCandles(IReadOnlyCollection<ICandle> candles)
        {
            if (!candles.Any())
            {
                return;
            }

            _healthService.TraceStartPersistCandles();

            try
            {
                var grouppedCandles = candles.GroupBy(c => c.AssetPairId);
                var tasks = new List<Task>();

                foreach (var group in grouppedCandles)
                {
                    tasks.Add(InsertAssetPairCandlesAsync(group, group.Key));
                }

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(nameof(CandlesPersistenceQueue), nameof(PersistCandles), "", ex);
                }
            }
            finally
            {
                _healthService.TraceStopPersistCandles();
            }
        }

        private async Task InsertAssetPairCandlesAsync(IEnumerable<ICandle> candles, string assetPairId)
        {
            var grouppedCandles = candles.GroupBy(c => new {c.PriceType, c.TimeInterval});

            foreach (var group in grouppedCandles)
            {
                try
                {
                    await _repository.InsertOrMergeAsync(
                        group.ToArray(),
                        assetPairId,
                        group.Key.PriceType,
                        group.Key.TimeInterval);
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(nameof(CandlesPersistenceQueue), nameof(InsertAssetPairCandlesAsync), "", ex);

                    await _failedToPersistCandlesPublisher.ProduceAsync(new FailedCandlesEnvelope
                    {
                        ProcessingMoment = DateTime.UtcNow,
                        Exception = ex.ToString(),
                        Candles = group
                    });
                }
            }
        }
    }
}
