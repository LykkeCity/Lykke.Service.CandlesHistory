using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesPersistenceQueue : 
        ProducerConsumer<Task>,
        ICandlesPersistenceQueue
    {
        private class FeedCandle : IFeedCandle
        {
            public string AssetPairId { get; set; }
            public PriceType PriceType { get; set; }
            public TimeInterval TimeInterval { get; set; }
            public DateTime DateTime { get; set; }
            public double Open { get; set; }
            public double Close { get; set; }
            public double High { get; set; }
            public double Low { get; set; }
            public bool IsBuy { get; set; }
        }

        public int PersistTasksQueueLength => _persistTasksQueueLength;
        public int CandlesToPersistQueueLength => _candlesToPersist.Count;

        private readonly ICandleHistoryRepository _repository;
        private readonly IFailedToPersistCandlesProducer _failedToPersistCandlesProducer;
        private readonly ILog _log;

        private readonly ConcurrentQueue<FeedCandle> _candlesToPersist;
        private int _persistTasksQueueLength;
        private TimeSpan _averagePersistTime;
        private DateTime _lastPerformanceLogMoment;

        public CandlesPersistenceQueue(
            ICandleHistoryRepository repository,
            IFailedToPersistCandlesProducer failedToPersistCandlesProducer,
            ILog log) :

            base(nameof(CandlesPersistenceQueue), log)
        {
            _repository = repository;
            _failedToPersistCandlesProducer = failedToPersistCandlesProducer;
            _log = log;
            _candlesToPersist = new ConcurrentQueue<FeedCandle>();
        }

        public void EnqueCandle(IFeedCandle candle, string assetPairId, PriceType priceType, TimeInterval timeInterval)
        {
            _candlesToPersist.Enqueue(new FeedCandle
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
        }

        public void Persist()
        {
            var candlesCount = _candlesToPersist.Count;

            if (candlesCount == 0)
            {
                return;
            }

            var candles = new List<FeedCandle>(candlesCount);

            for (var i = 0; i < Math.Min(candlesCount, 1000); i++)
            {
                if (_candlesToPersist.TryDequeue(out FeedCandle candle))
                {
                    candles.Add(candle);
                }
                else
                {
                    break;
                }
            }

            Interlocked.Increment(ref _persistTasksQueueLength);

            // Add processing task to producer/consumer's queue
            Produce(PersistCandles(candles));
        }

        protected override async Task Consume(Task task)
        {
            // On consume just await task
#if DEBUG
            await _log.WriteInfoAsync(nameof(CandlesPersistenceQueue), nameof(Consume), "", 
                $"Consuming task. Amount of tasks in queue={PersistTasksQueueLength}. Amount of candles in queue={_candlesToPersist.Count}");
#endif
            try
            {
                await task;
            }
            finally
            {
                Interlocked.Decrement(ref _persistTasksQueueLength);
            }
#if DEBUG
            await _log.WriteInfoAsync(nameof(CandlesPersistenceQueue), nameof(Consume), "", 
                $"Task is finished. Amount of tasks in queue={PersistTasksQueueLength}. Amount of candles in queue={_candlesToPersist.Count}");
#endif
        }

        private async Task PersistCandles(List<FeedCandle> candles)
        {
            if (!candles.Any())
            {
                return;
            }

            // Start persisting candles
            var sw = new Stopwatch();
            sw.Start();

            var grouppedCandles = candles
                .GroupBy(c => c.AssetPairId);
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

            // Update average write time and log service information
            sw.Stop();

            UpdatePerformanceStatistics(sw);
            await LogPerformanceStatistics();
        }

        private async Task InsertAssetPairCandlesAsync(string assetPairId, IEnumerable<FeedCandle> candles)
        {
            var grouppedCandles = candles.GroupBy(c => new {c.PriceType, c.TimeInterval});

            foreach (var group in grouppedCandles)
            {
                try
                {
                    await _repository.InsertOrMergeAsync(
                        group,
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

        private async Task LogPerformanceStatistics()
        {
            if (DateTime.UtcNow - _lastPerformanceLogMoment > TimeSpan.FromHours(1))
            {
                await _log.WriteInfoAsync(nameof(CandlesPersistenceQueue), nameof(PersistCandles), "", $"Average write time: {_averagePersistTime}");

                _lastPerformanceLogMoment = DateTime.UtcNow;
            }
        }

        private void UpdatePerformanceStatistics(Stopwatch sw)
        {
            _averagePersistTime = new TimeSpan((_averagePersistTime.Ticks + sw.Elapsed.Ticks) / 2);
        }
    }
}