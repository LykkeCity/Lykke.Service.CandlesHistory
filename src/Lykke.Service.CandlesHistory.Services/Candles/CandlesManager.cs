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

        #endregion
    }
}
