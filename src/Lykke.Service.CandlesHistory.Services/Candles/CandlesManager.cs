using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices.Repositories;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesManager : ICandlesManager
    {
        private static readonly ImmutableArray<TimeInterval> StoredIntervals = ImmutableArray.Create
        (
            TimeInterval.Sec,
            TimeInterval.Minute,
            TimeInterval.Min30,
            TimeInterval.Hour,
            TimeInterval.Day,
            TimeInterval.Week,
            TimeInterval.Month
        );

        private static readonly ImmutableArray<PriceType> StoredPriceTypes = ImmutableArray.Create
        (
            PriceType.Ask,
            PriceType.Bid,
            PriceType.Mid
        );

        private static readonly ImmutableDictionary<TimeInterval, TimeInterval> GetToStoredIntervalsMap = ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Min5, TimeInterval.Minute),
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Min15, TimeInterval.Minute),
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Min30, TimeInterval.Minute),
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Hour4, TimeInterval.Hour),
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Hour6, TimeInterval.Hour),
            new KeyValuePair<TimeInterval, TimeInterval>(TimeInterval.Hour12, TimeInterval.Hour)
        });

        private readonly IMidPriceQuoteGenerator _midPriceQuoteGenerator;
        private readonly ICachedCandlesHistoryService _cachedCandlesHistoryService;
        private readonly ICandleHistoryRepository _candleHistoryRepository;
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly ILog _log;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IImmutableDictionary<string, string> _candleHistoryAssetConnectionStrings;
        private readonly int _amountOfCandlesToStore;

        public CandlesManager(
            IMidPriceQuoteGenerator midPriceQuoteGenerator,
            ICachedCandlesHistoryService cachedCandlesHistoryService,
            ICandleHistoryRepository candleHistoryRepository,
            IAssetPairsManager assetPairsManager,
            ILog log,
            IDateTimeProvider dateTimeProvider,
            IImmutableDictionary<string, string> candleHistoryAssetConnectionStrings,
            int amountOfCandlesToStore)
        {
            _midPriceQuoteGenerator = midPriceQuoteGenerator;
            _cachedCandlesHistoryService = cachedCandlesHistoryService;
            _candleHistoryRepository = candleHistoryRepository;
            _assetPairsManager = assetPairsManager;
            _log = log;
            _dateTimeProvider = dateTimeProvider;
            _candleHistoryAssetConnectionStrings = candleHistoryAssetConnectionStrings;
            _amountOfCandlesToStore = amountOfCandlesToStore;
        }

        public void Start()
        {
            CacheCandlesAsync().Wait();
        }

        public async Task ProcessQuoteAsync(IQuote quote)
        {
            var assetPair = await _assetPairsManager.TryGetEnabledPairAsync(quote.AssetPair);

            if (assetPair == null)
            {
                return;
            }

            if (!_candleHistoryAssetConnectionStrings.ContainsKey(assetPair.Id))
            {
                return;
            }

            var quotePriceType = quote.IsBuy ? PriceType.Bid : PriceType.Ask;
            var midPriceQuote = _midPriceQuoteGenerator.TryGenerate(quote, assetPair.Accuracy);

            foreach (var timeInterval in StoredIntervals)
            {
                _cachedCandlesHistoryService.AddQuote(quote, quotePriceType, timeInterval);
                
                if (midPriceQuote != null)
                {
                    _cachedCandlesHistoryService.AddQuote(midPriceQuote, PriceType.Mid, timeInterval);
                }
            }
        }

        /// <summary>
        /// Obtains candles history from cache, doing time interval remap and read persistent history if needed
        /// </summary>
        public async Task<IEnumerable<IFeedCandle>> GetCandlesAsync(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            var alignedFromMoment = fromMoment.RoundTo(timeInterval);
            var alignedToMoment = toMoment.RoundTo(timeInterval);

            if (StoredIntervals.Contains(timeInterval))
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
        private async Task<IEnumerable<IFeedCandle>> GetStoredCandlesAsync(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            var cachedHistory = _cachedCandlesHistoryService
                .GetCandles(assetPairId, priceType, timeInterval, fromMoment, toMoment)
                .ToArray();
            var oldestCachedCandle = cachedHistory.FirstOrDefault();

            // If cache empty or even oldest cached candle DateTime is after fromMoment try to read persistent history
            if (oldestCachedCandle == null || oldestCachedCandle.DateTime > fromMoment)
            {
                var newToMoment = oldestCachedCandle?.DateTime ?? toMoment;
                var persistentHistory = await _candleHistoryRepository.GetCandlesAsync(assetPairId, timeInterval,
                    priceType, fromMoment, newToMoment);

                // Concatenating persistent and cached history
                return persistentHistory
                    // If at least one candle is cached, persistent history used only up to the oldest of cached candle
                    .TakeWhile(c => oldestCachedCandle == null || c.DateTime < oldestCachedCandle.DateTime)
                    .Concat(cachedHistory);
            }

            // Cache not empty and it contains fromMoment candle, so we don't need to read persistent history
            return cachedHistory;
        }

        private async Task CacheCandlesAsync()
        {
            await _log.WriteInfoAsync(Constants.ComponentName, null, null, "Caching candles history...");

            var assetPairs = await _assetPairsManager.GetAllEnabledAsync();
            var now = _dateTimeProvider.UtcNow;
            var cacheAssetPairTasks = assetPairs
                .Where(a => _candleHistoryAssetConnectionStrings.ContainsKey(a.Id))
                .Select(assetPair => CacheAssetPairCandlesAsync(assetPair, now));

            await Task.WhenAll(cacheAssetPairTasks);

            await _log.WriteInfoAsync(Constants.ComponentName, null, null, "All candles history is cached");
        }

        private async Task CacheAssetPairCandlesAsync(IAssetPair assetPair, DateTime toDate)
        {
            await _log.WriteInfoAsync(Constants.ComponentName, null, null, $"Caching {assetPair.Id} candles history...");

            foreach (var priceType in StoredPriceTypes)
            {
                foreach (var timeInterval in StoredIntervals)
                {
                    var alignedToDate = toDate.RoundTo(timeInterval);
                    var alignedFromDate = alignedToDate.AddIntervalTicks(-_amountOfCandlesToStore, timeInterval);
                    var candles = await _candleHistoryRepository.GetCandlesAsync(assetPair.Id, timeInterval, priceType, alignedFromDate, alignedToDate);

                    _cachedCandlesHistoryService.InitializeHistory(assetPair.Id, timeInterval, priceType, candles);
                }
            }

            await _log.WriteInfoAsync(Constants.ComponentName, null, null, $"{assetPair.Id} candles history is cached");
        }
    }
}