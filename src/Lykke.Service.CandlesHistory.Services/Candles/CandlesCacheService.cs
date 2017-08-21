using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesCacheService : ICandlesCacheService
    {
        private readonly int _amountOfCandlesToStore;
        private readonly ILog _log;

        /// <summary>
        /// Candles keyed by asset ID, price type and time interval type are sorted by DateTime 
        /// </summary>
        private readonly ConcurrentDictionary<string, LinkedList<IFeedCandle>> _candles;

        public CandlesCacheService(int amountOfCandlesToStore, ILog log)
        {
            _amountOfCandlesToStore = amountOfCandlesToStore;
            _log = log;
            _candles = new ConcurrentDictionary<string, LinkedList<IFeedCandle>>();
        }

        public void InitializeHistory(string assetPairId, TimeInterval timeInterval, PriceType priceType, IEnumerable<IFeedCandle> candles)
        {
            var key = GetKey(assetPairId, priceType, timeInterval);
            var candlesList = new LinkedList<IFeedCandle>(candles.Limit(_amountOfCandlesToStore));

            foreach (var candle in candlesList)
            {
                if (candle.DateTime.Kind != DateTimeKind.Utc)
                {
                    throw new InvalidOperationException($"Candle {candle.ToJson()} DateTime.Kind is {candle.DateTime.Kind}, but should be Utc");
                }
            }

            _candles.TryAdd(key, candlesList);
        }

        public void AddCandle(IFeedCandle candle, string assetPairId, PriceType priceType, TimeInterval timeInterval)
        {
            var key = GetKey(assetPairId, priceType, timeInterval);

            _candles.AddOrUpdate(key,
                addValueFactory: k => AddNewCandlesHistory(candle),
                updateValueFactory: (k, hisotry) => UpdateCandlesHistory(hisotry, candle, assetPairId, priceType, timeInterval));
        }

        public IEnumerable<IFeedCandle> GetCandles(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            if (fromMoment.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"Date kind should be Utc, but it is {fromMoment.Kind}", nameof(fromMoment));
            }
            if (toMoment.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"Date kind should be Utc, but it is {toMoment.Kind}", nameof(toMoment));
            }

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
                    .TakeWhile(i => i.DateTime < toMoment);
            }

            return Enumerable.Empty<IFeedCandle>();
        }

        private static LinkedList<IFeedCandle> AddNewCandlesHistory(IFeedCandle candle)
        {
            var history = new LinkedList<IFeedCandle>();
            
            history.AddLast(candle);

            return history;
        }

        private LinkedList<IFeedCandle> UpdateCandlesHistory(LinkedList<IFeedCandle> history, IFeedCandle candle, string assetPairId, PriceType priceType, TimeInterval timeInterval)
        {
            lock (history)
            {
                // Starting from the latest candle, moving down to the history
                for (var node = history.Last; node != null; node = node.Previous)
                {
                    // Candle at given point already exists, so we merging they
                    if (node.Value.DateTime == candle.DateTime)
                    {
                        node.Value = node.Value.MergeWith(candle);

                        break;
                    }

                    // If we found more early point than candle,
                    // that's the point after which we should add candle
                    if (node.Value.DateTime < candle.DateTime)
                    {
                        history.AddAfter(node, candle);

                        // Should we remove oldest candle?
                        if (history.Count > _amountOfCandlesToStore)
                        {
                            history.RemoveFirst();
                        }

                        break;
                    }

                    // If we achieve first node, we should check if cache is full or not
                    if (node == history.First)
                    {
                        if (history.Count < _amountOfCandlesToStore)
                        {
                            // Cache is not full, so we can store the candle as earliest point in history
                            history.AddBefore(history.First, candle);
                        }
                      
                        // Cache is full, so we can't store the candle there, because, probably
                        // there is persisted candle at this point
                        _log.WriteWarningAsync(
                            nameof(CandlesCacheService), 
                            nameof(UpdateCandlesHistory),
                            candle.ToJson(),
                            $"Can't cache candle it's too old to store in cache. Current history length for {assetPairId}:{priceType}:{timeInterval} = {history.Count}");
                        break;
                    }
                }
            }

            return history;
        }

        private static string GetKey(string assetPairId, PriceType priceType, TimeInterval timeInterval)
        {
            return $"{assetPairId.Trim().ToUpper()}-{priceType}-{timeInterval}";
        }
    }
}