﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
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

        public CandlesManager(
            ICandlesCacheService candlesCacheService,
            ICandlesHistoryRepository candlesHistoryRepository)
        {
            _candlesCacheService = candlesCacheService;
            _candlesHistoryRepository = candlesHistoryRepository;
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

            if (alignedFromMoment == alignedToMoment)
            {
                alignedToMoment = alignedFromMoment.AddIntervalTicks(1, timeInterval);
            }

            if (Constants.StoredIntervals.Contains(timeInterval))
            {
                return await GetStoredCandlesAsync(assetPairId, priceType, timeInterval, alignedFromMoment, alignedToMoment);
            }

            var sourceInterval = GetToStoredIntervalsMap[timeInterval];
            var sourceHistory = await GetStoredCandlesAsync(assetPairId, priceType, sourceInterval, alignedFromMoment, alignedToMoment);

            // Merging candles from sourceInterval (e.g. Minute) to bigger timeInterval (e.g. Min15)
            return CandlesMerger.MergeIntoBiggerIntervals(sourceHistory, timeInterval);
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
            var cachedHistory = (await _candlesCacheService
                .GetCandlesAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment))
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

        private (DateTime fromMomentInclusive, DateTime toMomentExclusive) GetQueryTimeRange(
            CandleTimeInterval interval, int ticksToPast)
        {
            var to = DateTime.UtcNow
                .TruncateTo(interval)
                .AddIntervalTicks(1, interval);
            var from = to
                .AddIntervalTicks(-ticksToPast - 1, interval);

            return (fromMomentInclusive: from, toMomentExclusive: to);
        }
    }
}
