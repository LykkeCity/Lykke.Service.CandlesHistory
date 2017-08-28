using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using IDateTimeProvider = Lykke.Service.CandlesHistory.Core.Services.IDateTimeProvider;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesCacheInitalizationService : ICandlesCacheInitalizationService
    {
        private readonly ILog _log;
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMidPriceQuoteGenerator _midPriceQuoteGenerator;
        private readonly ICandlesCacheService _candlesCacheService;
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;
        private readonly int _amountOfCandlesToStore;

        public CandlesCacheInitalizationService(
            ILog log,
            IAssetPairsManager assetPairsManager,
            IDateTimeProvider dateTimeProvider,
            IMidPriceQuoteGenerator midPriceQuoteGenerator,
            ICandlesCacheService candlesCacheService,
            ICandlesHistoryRepository candlesHistoryRepository,
            int amountOfCandlesToStore)
        {
            _log = log;
            _assetPairsManager = assetPairsManager;
            _dateTimeProvider = dateTimeProvider;
            _midPriceQuoteGenerator = midPriceQuoteGenerator;
            _candlesCacheService = candlesCacheService;
            _candlesHistoryRepository = candlesHistoryRepository;
            _amountOfCandlesToStore = amountOfCandlesToStore;
        }

        public async Task InitializeCacheAsync()
        {
            await _log.WriteInfoAsync(nameof(CandlesCacheInitalizationService), nameof(InitializeCacheAsync), null, "Caching candles history...");

            var assetPairs = await _assetPairsManager.GetAllEnabledAsync();
            var now = _dateTimeProvider.UtcNow;
            var cacheAssetPairTasks = assetPairs
                .Where(a => _candlesHistoryRepository.CanStoreAssetPair(a.Id))
                .Select(assetPair => CacheAssetPairCandlesAsync(assetPair, now));

            await Task.WhenAll(cacheAssetPairTasks);

            await _log.WriteInfoAsync(nameof(CandlesCacheInitalizationService), nameof(InitializeCacheAsync), null, "All candles history is cached");
        }

        private async Task CacheAssetPairCandlesAsync(IAssetPair assetPair, DateTime toDate)
        {
            await _log.WriteInfoAsync(nameof(CandlesCacheInitalizationService), nameof(InitializeCacheAsync), null, $"Caching {assetPair.Id} candles history...");

            foreach (var priceType in Constants.StoredPriceTypes)
            {
                foreach (var timeInterval in Constants.StoredIntervals)
                {
                    var alignedToDate = toDate.RoundTo(timeInterval);
                    var alignedFromDate = alignedToDate.AddIntervalTicks(-_amountOfCandlesToStore, timeInterval);
                    var candles = (await _candlesHistoryRepository.GetCandlesAsync(assetPair.Id, timeInterval, priceType, alignedFromDate, alignedToDate))
                        .ToArray();

                    if ((priceType == PriceType.Ask || priceType == PriceType.Bid) && timeInterval == TimeInterval.Sec)
                    {
                        var lastCandle = candles.LastOrDefault();
                        if (lastCandle != null)
                        {
                            _midPriceQuoteGenerator.Initialize(assetPair.Id, priceType == PriceType.Bid, lastCandle.Close, lastCandle.DateTime);
                        }
                    }

                    _candlesCacheService.Initialize(assetPair.Id, timeInterval, priceType, candles);
                }
            }

            await _log.WriteInfoAsync(nameof(CandlesCacheInitalizationService), nameof(InitializeCacheAsync), null, $"{assetPair.Id} candles history is cached");
        }
    }
}