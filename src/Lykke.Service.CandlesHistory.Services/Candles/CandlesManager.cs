using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesManager : ICandlesManager
    {
        private static readonly ImmutableDictionary<CandleTimeInterval, CandleTimeInterval> GetToStoredIntervalsMap = ImmutableDictionary.CreateRange(new[]
        {
            KeyValuePair.Create(CandleTimeInterval.Min5, CandleTimeInterval.Minute),
            KeyValuePair.Create(CandleTimeInterval.Min15, CandleTimeInterval.Minute),
            KeyValuePair.Create(CandleTimeInterval.Min30, CandleTimeInterval.Minute),
            KeyValuePair.Create(CandleTimeInterval.Hour4, CandleTimeInterval.Hour),
            KeyValuePair.Create(CandleTimeInterval.Hour6, CandleTimeInterval.Hour),
            KeyValuePair.Create(CandleTimeInterval.Hour12, CandleTimeInterval.Hour)
        });

        private readonly ICandlesCacheService _candlesCacheService;
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;
        private readonly ICandlesPersistenceQueue _candlesPersistenceQueue;

        public CandlesManager(
            ICandlesCacheService candlesCacheService,
            ICandlesHistoryRepository candlesHistoryRepository,
            ICandlesPersistenceQueue candlesPersistenceQueue)
        {
            _candlesCacheService = candlesCacheService;
            _candlesHistoryRepository = candlesHistoryRepository;
            _candlesPersistenceQueue = candlesPersistenceQueue;
        }

        public void ProcessCandle(ICandle candle)
        {
            try
            {
                if (!_candlesHistoryRepository.CanStoreAssetPair(candle.AssetPairId))
                {
                    return;
                }

                if (!Constants.StoredIntervals.Contains(candle.TimeInterval))
                {
                    return;
                }
                
                _candlesCacheService.Cache(candle);
                _candlesPersistenceQueue.EnqueueCandle(candle);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to process candle: {candle.ToJson()}", ex);
            }
        }

        /// <summary>
        /// Obtains candles history from cache, doing time interval remap and read persistent history if needed
        /// </summary>
        public async Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            if (!_candlesHistoryRepository.CanStoreAssetPair(assetPairId))
            {
                throw new InvalidOperationException($"Connection string for asset pair {assetPairId} not configured");
            }

            var alignedFromMoment = fromMoment.TruncateTo(timeInterval);
            var alignedToMoment = toMoment.TruncateTo(timeInterval);

            if (Constants.StoredIntervals.Contains(timeInterval))
            {
                return await GetStoredCandlesAsync(assetPairId, priceType, timeInterval, alignedFromMoment, alignedToMoment);
            }

            var sourceInterval = GetToStoredIntervalsMap[timeInterval];
            var sourceHistory = await GetStoredCandlesAsync(assetPairId, priceType, sourceInterval, alignedFromMoment, alignedToMoment);

            // Merging candles from sourceInterval (e.g. Minute) to bigger timeInterval (e.g. Min15)
            return CandlesMerger.MergeIntoBiggerIntervals(sourceHistory, timeInterval);
        }

        /// <summary>
        /// Obtains candles history from cache only in stored time intervals, reading persistent history if needed
        /// </summary>
        /// <param name="assetPairId"></param>
        /// <param name="priceType"></param>
        /// <param name="timeInterval"></param>
        /// <param name="fromMoment"></param>
        /// <param name="toMoment"></param>
        /// <returns></returns>
        private async Task<IEnumerable<ICandle>> GetStoredCandlesAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            var cachedHistory = _candlesCacheService
                .GetCandles(assetPairId, priceType, timeInterval, fromMoment, toMoment)
                .ToArray();
            var oldestCachedCandle = cachedHistory.FirstOrDefault();

            // If cache is empty or even oldest cached candle DateTime is after fromMoment and assetPairs has connection string, 
            // then try to read persistent history
            if (oldestCachedCandle == null || oldestCachedCandle.Timestamp > fromMoment)
            {
                var newToMoment = oldestCachedCandle?.Timestamp ?? toMoment;
                var persistentHistory = await _candlesHistoryRepository.GetCandlesAsync(assetPairId, timeInterval, priceType, fromMoment, newToMoment);

                // Concatenating persistent and cached history
                return persistentHistory
                    // If at least one candle is cached, persistent history used only up to the oldest of cached candle
                    .TakeWhile(c => oldestCachedCandle == null || c.Timestamp < oldestCachedCandle.Timestamp)
                    .Concat(cachedHistory);
            }

            // Cache not empty and it contains fromMoment candle, so we don't need to read persistent history
            return cachedHistory;
        }
    }
}
