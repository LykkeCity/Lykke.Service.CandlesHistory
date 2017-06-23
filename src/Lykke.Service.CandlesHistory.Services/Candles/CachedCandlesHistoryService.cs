﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Common;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CachedCandlesHistoryService : ICachedCandlesHistoryService
    {
        private readonly int _amountOfCandlesToStore;
        /// <summary>
        /// Candles keyed by asset ID, price type and time interval type are sorted by DateTime 
        /// </summary>
        private readonly ConcurrentDictionary<string, LinkedList<IFeedCandle>> _candles;

        public CachedCandlesHistoryService(int amountOfCandlesToStore)
        {
            _amountOfCandlesToStore = amountOfCandlesToStore;
            _candles = new ConcurrentDictionary<string, LinkedList<IFeedCandle>>();
        }

        public void InitializeHistory(string assetPairId, TimeInterval timeInterval, PriceType priceType, IEnumerable<IFeedCandle> candles)
        {
            var key = GetKey(assetPairId, priceType, timeInterval);

            _candles.TryAdd(key, new LinkedList<IFeedCandle>(candles.Limit(_amountOfCandlesToStore)));
        }

        public void AddQuote(IQuote quote, PriceType priceType, TimeInterval timeInterval)
        {
            var key = GetKey(quote.AssetPair, priceType, timeInterval);

            _candles.AddOrUpdate(key,
                addValueFactory: k => AddNewCandlesHistory(quote, timeInterval),
                updateValueFactory: (k, hisotry) => UpdateCandlesHistory(hisotry, quote, timeInterval));
        }

        public IEnumerable<IFeedCandle> GetCandles(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            var key = GetKey(assetPairId, priceType, timeInterval);

            if (_candles.TryGetValue(key, out LinkedList<IFeedCandle> history))
            {
                IReadOnlyCollection<IFeedCandle> localHistory;
                lock (history)
                {
                    localHistory = history.ToArray();
                }

                // TODO: binnary search could increase speed
                return localHistory
                    .SkipWhile(i => i.DateTime < fromMoment)
                    .TakeWhile(i => i.DateTime <= toMoment);
            }

            return Enumerable.Empty<IFeedCandle>();
        }

        private LinkedList<IFeedCandle> AddNewCandlesHistory(IQuote quote, TimeInterval timeInterval)
        {
            var history = new LinkedList<IFeedCandle>();
            var intervalDate = quote.Timestamp.RoundTo(timeInterval);

            history.AddLast(quote.ToCandle(intervalDate));

            return history;
        }

        private LinkedList<IFeedCandle> UpdateCandlesHistory(LinkedList<IFeedCandle> history, IQuote quote, TimeInterval timeInterval)
        {
            var intervalDate = quote.Timestamp.RoundTo(timeInterval);
            var newCandle = quote.ToCandle(intervalDate);

            lock (history)
            {
                // Starting from the latest candle, moving down to the history
                for (var node = history.Last; node != null; node = node.Previous)
                {
                    // Candle at given point already exists, so we merging they
                    if (node.Value.DateTime == newCandle.DateTime)
                    {
                        node.Value = node.Value.MergeWith(newCandle);
                        break;
                    }

                    // If we found more early point than newCandle has,
                    // that's the point after which we should add newCandle
                    if (node.Value.DateTime < intervalDate)
                    {
                        history.AddAfter(node, newCandle);

                        // Should we remove oldest candle?
                        if (history.Count > _amountOfCandlesToStore)
                        {
                            history.RemoveFirst();
                        }

                        break;
                    }
                }
            }

            return history;
        }

        private static string GetKey(string assetPairId, PriceType priceType, TimeInterval timeInterval)
        {
            return $"{assetPairId}-{priceType}-{timeInterval}";
        }
    }
}