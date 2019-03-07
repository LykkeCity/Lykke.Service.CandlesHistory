using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesManager : ICandlesManager
    {
        private readonly ICandlesCacheService _candlesCacheService;
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;

        #region Initialization

        public CandlesManager(
            ICandlesCacheService candlesCacheService,
            ICandlesHistoryRepository candlesHistoryRepository)
        {
            _candlesCacheService = candlesCacheService;
            _candlesHistoryRepository = candlesHistoryRepository;
        }

        #endregion

        #region Public

        /// <summary>
        /// Obtains candles history from cache, doing time interval remap and read persistent history if needed
        /// </summary>
        public async Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment, SlotType? activeSlot = null)
        {
            CheckupAssetPairOrFail(assetPairId);

            var alignedFromMoment = fromMoment.TruncateTo(timeInterval);
            var alignedToMoment = toMoment.TruncateTo(timeInterval);

            if (alignedFromMoment == alignedToMoment)
            {
                alignedToMoment = alignedFromMoment.AddIntervalTicks(1, timeInterval);
            }

            if (!activeSlot.HasValue)
                activeSlot = await GetActiveSlotAsync();

            if (Constants.StoredIntervals.Contains(timeInterval))
            {
                return await GetStoredCandlesAsync(assetPairId, priceType, timeInterval, alignedFromMoment, alignedToMoment, activeSlot.Value);
            }

            return Array.Empty<ICandle>();
        }

        /// <inheritdoc cref="ICandlesManager"/>
        public async Task<(decimal TradingVolume, decimal OppositeTradingVolume)> GetSummaryTradingVolumesAsync(string assetPairId, CandleTimeInterval interval, int ticksToPast)
        {
            var (fromMomentInclusive, toMomentExclusive) = GetQueryTimeRange(interval, ticksToPast);

            var candles = await GetCandlesAsync(assetPairId, CandlePriceType.Trades, interval, fromMomentInclusive, toMomentExclusive);

            var volume = 0.0M;
            var oppositeVolume = 0.0M;

            foreach (var candle in candles)
            {
                volume += (decimal)candle.TradingVolume;
                oppositeVolume += (decimal)candle.TradingOppositeVolume;
            }

            return (TradingVolume: volume, OppositeTradingVolume: oppositeVolume);
        }

        /// <inheritdoc cref="ICandlesManager"/>
        public async Task<decimal> GetLastTradePriceAsync(string assetPairId, CandleTimeInterval interval, int ticksToPast)
        {
            var (fromMomentInclusive, toMomentExclusive) = GetQueryTimeRange(interval, ticksToPast);

            var candle = (await GetCandlesAsync(assetPairId, CandlePriceType.Trades, interval, fromMomentInclusive, toMomentExclusive))
                .LastOrDefault();

            return candle != null
                ? (decimal)candle.Close
                : 0.0M;
        }

        /// <inheritdoc cref="ICandlesManager"/>
        public async Task<decimal> GetTradePriceChangeAsync(string assetPairId, CandleTimeInterval interval, int ticksToPast)
        {
            var (fromMomentInclusive, toMomentExclusive) = GetQueryTimeRange(interval, ticksToPast);

            var candles = (await GetCandlesAsync(assetPairId, CandlePriceType.Trades, interval, fromMomentInclusive, toMomentExclusive))
                .ToArray();

            if (!candles.Any())
                return 0.0M;

            var firstCandle = candles.First();
            var lastCandle = candles.Last();

            try
            {
                return (decimal)((lastCandle.Close - firstCandle.Open) / firstCandle.Open);
            }
            catch // Division by zero.
            {
                return 0.0M;
            }
        }

        /// <summary>
        /// Finds out the oldest stored candle's timestamp (if any).
        /// </summary>
        /// <param name="assetPairId"></param>
        /// <param name="priceType"></param>
        /// <param name="interval"></param>
        /// <returns>The oldest candle or null./></returns>
        /// <exception cref="InvalidOperationException">If the specified asset pair is not currently supported by storage.</exception>
        public async Task<ICandle> TryGetOldestCandleAsync(string assetPairId, CandlePriceType priceType,
            CandleTimeInterval interval)
        {
            CheckupAssetPairOrFail(assetPairId);

            var firstCandle = await _candlesHistoryRepository.TryGetFirstCandleAsync(assetPairId, interval, priceType);
            
            return firstCandle; // The risk of the null is minimal but not excluded.
        }

        public Task<SlotType> GetActiveSlotAsync()
        {
            return _candlesCacheService.GetActiveSlotAsync();
        }

        #endregion

        #region Private

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the specified asset pair is not supported.
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        private void CheckupAssetPairOrFail(string assetPairId)
        {
            if (!_candlesHistoryRepository.CanStoreAssetPair(assetPairId))
                throw new InvalidOperationException($"Connection string for asset pair {assetPairId} not configured");
        }

        /// <summary>
        /// Obtains candles history from cache only in stored time intervals
        /// </summary>
        /// <param name="assetPairId"></param>
        /// <param name="priceType"></param>
        /// <param name="timeInterval"></param>
        /// <param name="fromMoment"></param>
        /// <param name="toMoment"></param>
        /// <param name="activeSlot"></param>
        /// <returns></returns>
        private async Task<IEnumerable<ICandle>> GetStoredCandlesAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment, SlotType activeSlot)
        {
            var cachedHistory = (await _candlesCacheService
                .GetCandlesAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, activeSlot))
                .ToArray();

            return cachedHistory;
        }

        private static (DateTime fromMomentInclusive, DateTime toMomentExclusive) GetQueryTimeRange(
            CandleTimeInterval interval, int ticksToPast)
        {
            var to = DateTime.UtcNow
                .TruncateTo(interval)
                .AddIntervalTicks(1, interval);
            var from = to
                .AddIntervalTicks(-ticksToPast - 1, interval);

            return (fromMomentInclusive: from, toMomentExclusive: to);
        }

        #endregion
    }
}
