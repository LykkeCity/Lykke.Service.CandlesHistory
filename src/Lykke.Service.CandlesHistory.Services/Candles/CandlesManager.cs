﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices.Repositories;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesManager : ICandlesManager
    {
        private static readonly TimeInterval[] StoredIntervals =
        {
            TimeInterval.Sec,
            TimeInterval.Minute,
            TimeInterval.Min30,
            TimeInterval.Hour,
            TimeInterval.Day,
            TimeInterval.Week,
            TimeInterval.Month
        };

        private static readonly PriceType[] StoredPriceTypes =
        {
            PriceType.Ask,
            PriceType.Bid,
            PriceType.Mid
        };

        private readonly IMidPriceQuoteGenerator _midPriceQuoteGenerator;
        private readonly ICandlesService _candlesService;
        private readonly ICandleHistoryRepository _candleHistoryRepository;
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly ILog _log;
        private readonly IImmutableDictionary<string, string> _candleHistoryAssetConnectionStrings;
        private readonly int _amountOfCandlesToStore;

        public CandlesManager(
            IMidPriceQuoteGenerator midPriceQuoteGenerator,
            ICandlesService candlesService,
            ICandleHistoryRepository candleHistoryRepository,
            IAssetPairsManager assetPairsManager,
            ILog log,
            IImmutableDictionary<string, string> candleHistoryAssetConnectionStrings,
            int amountOfCandlesToStore)
        {
            _midPriceQuoteGenerator = midPriceQuoteGenerator;
            _candlesService = candlesService;
            _candleHistoryRepository = candleHistoryRepository;
            _assetPairsManager = assetPairsManager;
            _log = log;
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

            if (assetPair == null || assetPair.IsDisabled)
            {
                return;
            }

            if (!_candleHistoryAssetConnectionStrings.ContainsKey(assetPair.Id))
            {
                return;
            }

            var quotePriceType = quote.IsBuy ? PriceType.Bid : PriceType.Ask;
            var midPriceQuote = _midPriceQuoteGenerator.TryGenerate(quote, assetPair);

            foreach (var timeInterval in StoredIntervals)
            {
                _candlesService.AddQuote(quote, quotePriceType, timeInterval);
                
                if (midPriceQuote != null)
                {
                    _candlesService.AddQuote(midPriceQuote, PriceType.Mid, timeInterval);
                }
            }
        }

        private async Task CacheCandlesAsync()
        {
            await _log.WriteInfoAsync(Constants.ComponentName, null, null, "Caching candles history...");

            var assetPairs = await _assetPairsManager.GetAllEnabledAsync();
            var now = DateTime.UtcNow;
            var cacheAssetPairTasks = assetPairs
                .Where(a => _candleHistoryAssetConnectionStrings.ContainsKey(a.Id))
                .Select(assetPair => CacheAssetPairCandlesAsync(assetPair, now));

            await Task.WhenAll(cacheAssetPairTasks);

            await _log.WriteInfoAsync(Constants.ComponentName, null, null, "All candles history is cached");
        }

        private async Task CacheAssetPairCandlesAsync(IAssetPair assetPair, DateTime upToDate)
        {
            await _log.WriteInfoAsync(Constants.ComponentName, null, null, $"Caching {assetPair.Id} candles history...");

            foreach (var priceType in StoredPriceTypes)
            {
                foreach (var timeInterval in StoredIntervals)
                {
                    var fromDate = upToDate.AddIntervalTicks(-_amountOfCandlesToStore, timeInterval);
                    var candles = await _candleHistoryRepository.GetCandlesAsync(assetPair.Id, timeInterval, priceType, fromDate, upToDate);

                    _candlesService.InitializeHistory(assetPair, priceType, timeInterval, candles);
                }
            }

            await _log.WriteInfoAsync(Constants.ComponentName, null, null, $"{assetPair.Id} candles history is cached");
        }
    }
}