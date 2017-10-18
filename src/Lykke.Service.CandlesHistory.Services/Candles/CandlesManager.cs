using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesManager : ICandlesManager
    {
        private static readonly ImmutableDictionary<TimeInterval, TimeInterval> GetToStoredIntervalsMap = ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Min5, TimeInterval.Minute),
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Min15, TimeInterval.Minute),
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Min30, TimeInterval.Minute),
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Hour4, TimeInterval.Hour),
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Hour6, TimeInterval.Hour),
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Hour12, TimeInterval.Hour)
        });

        private readonly ICandlesCacheService _candlesCacheService;
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly ICandlesPersistenceQueue _candlesPersistenceQueue;

        public CandlesManager(
            ICandlesCacheService candlesCacheService,
            ICandlesHistoryRepository candlesHistoryRepository,
            IAssetPairsManager assetPairsManager,
            ICandlesPersistenceQueue candlesPersistenceQueue)
        {
            _candlesCacheService = candlesCacheService;
            _candlesHistoryRepository = candlesHistoryRepository;
            _assetPairsManager = assetPairsManager;
            _candlesPersistenceQueue = candlesPersistenceQueue;
        }

        public async Task ProcessCandleAsync(ICandle candle)
        {
            try
            {
                if (!_candlesHistoryRepository.CanStoreAssetPair(candle.AssetPairId))
                {
                    return;
                }

                var assetPair = await _assetPairsManager.TryGetEnabledPairAsync(candle.AssetPairId);

                if (assetPair == null)
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
        public async Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            if (!_candlesHistoryRepository.CanStoreAssetPair(assetPairId))
            {
                throw new InvalidOperationException($"Connection string for asset pair {assetPairId} not configured");
            }
            if (await _assetPairsManager.TryGetEnabledPairAsync(assetPairId) == null)
            {
                throw new InvalidOperationException($"Asset pair {assetPairId} not found or disabled in dictionary");
            }

            var alignedFromMoment = fromMoment.RoundTo(timeInterval);
            var alignedToMoment = toMoment.RoundTo(timeInterval);

            if (Constants.StoredIntervals.Contains(timeInterval))
            {
                return await GetStoredCandlesAsync(assetPairId, priceType, timeInterval, alignedFromMoment, alignedToMoment);
            }

            var sourceInterval = GetToStoredIntervalsMap[timeInterval];
            var sourceHistory = await GetStoredCandlesAsync(assetPairId, priceType, sourceInterval, alignedFromMoment, alignedToMoment);

            // Merging candles from sourceInterval (e.g. Minute) to bigger timeInterval (e.g. Min15)
            return sourceHistory.MergeIntoBiggerIntervals(timeInterval);
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
        private async Task<IEnumerable<ICandle>> GetStoredCandlesAsync(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            var cachedHistory = _candlesCacheService
                .GetCandles(assetPairId, priceType, timeInterval, fromMoment, toMoment)
                .ToArray();
            var oldestCachedCandle = cachedHistory.FirstOrDefault();

            // If cache empty or even oldest cached candle DateTime is after fromMoment and assetPairs has connection string, 
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