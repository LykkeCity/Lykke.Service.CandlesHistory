using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesPersistenceQueue : 
        ProducerConsumer<IReadOnlyCollection<AssetPairCandle>>,
        ICandlesPersistenceQueue
    {
        private readonly ICandleHistoryRepository _repository;
        private readonly IFailedToPersistCandlesProducer _failedToPersistCandlesProducer;
        private readonly ILog _log;
        private readonly IHealthService _healthService;
        private readonly ApplicationSettings.PersistenceSettings _settings;

        private readonly ConcurrentQueue<AssetPairCandle> _candlesToDispatch;
        
        public CandlesPersistenceQueue(
            ICandleHistoryRepository repository,
            IFailedToPersistCandlesProducer failedToPersistCandlesProducer,
            ILog log,
            IHealthService healthService,
            ApplicationSettings.PersistenceSettings settings) :

            base(nameof(CandlesPersistenceQueue), log)
        {
            _repository = repository;
            _failedToPersistCandlesProducer = failedToPersistCandlesProducer;
            _log = log;
            _healthService = healthService;
            _settings = settings;
            _candlesToDispatch = new ConcurrentQueue<AssetPairCandle>();
        }

        public void EnqueueCandle(IFeedCandle candle, string assetPairId, PriceType priceType, TimeInterval timeInterval)
        {
            _candlesToDispatch.Enqueue(new AssetPairCandle
            {
                AssetPairId = assetPairId,
                PriceType = priceType,
                TimeInterval = timeInterval,
                DateTime = candle.DateTime,
                Open = candle.Open,
                Close = candle.Close,
                Low = candle.Low,
                High = candle.High,
                IsBuy = candle.IsBuy
            });

            _healthService.TraceEnqueueCandle();
        }

        public bool DispatchCandlesToPersist()
        {
            var candlesCount = _candlesToDispatch.Count;

            if (candlesCount == 0)
            {
                return false;
            }

            var candles = new List<AssetPairCandle>(candlesCount);

            for (var i = 0; i < Math.Min(candlesCount, _settings.MaxBatchSize); i++)
            {
                if (_candlesToDispatch.TryDequeue(out AssetPairCandle candle))
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

            return true;
        }

        protected override async Task Consume(IReadOnlyCollection<AssetPairCandle> candles)
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
                _healthService.TraceCandlesBatchPersisted();
            }
#if DEBUG
            await _log.WriteInfoAsync(nameof(CandlesPersistenceQueue), nameof(Consume), "", 
                $"Candles batch with {candles.Count} candles is persisted. Amount of batches in queue={_healthService.BatchesToPersistQueueLength}. Amount of candles to dispath={_healthService.CandlesToDispatchQueueLength}");
#endif
        }

        private async Task PersistCandles(IReadOnlyCollection<AssetPairCandle> candles)
        {
            if (!candles.Any())
            {
                return;
            }

            _healthService.TraceStartPersistCandles(candles.Count);

            try
            {
                var grouppedCandles = candles.GroupBy(c => c.AssetPairId);
                var tasks = new List<Task>();

                foreach (var group in grouppedCandles)
                {
                    tasks.Add(InsertAssetPairCandlesAsync(group.Key, group));
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

        private async Task InsertAssetPairCandlesAsync(string assetPairId, IEnumerable<AssetPairCandle> candles)
        {
            var grouppedCandles = candles.GroupBy(c => new {c.PriceType, c.TimeInterval});

            foreach (var group in grouppedCandles)
            {
                try
                {
                    var mergedCandles = group
                        .GroupBy(c => c.DateTime)
                        .Select(g => g.MergeAll());

                    await _repository.InsertOrMergeAsync(
                        mergedCandles,
                        assetPairId,
                        group.Key.TimeInterval,
                        group.Key.PriceType);
                }
                catch (Exception ex)
                {
                    await _failedToPersistCandlesProducer.ProduceAsync(new FailedCandlesEnvelope
                    {
                        ProcessingMoment = DateTime.UtcNow,
                        Exception = ex.ToString(),
                        AssetPairId = assetPairId,
                        TimeInterval = group.Key.TimeInterval,
                        PriceType = group.Key.PriceType,
                        Candles = group
                    });
                }
            }
        }
    }
}