using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Common;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
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
        private ConcurrentDictionary<string, LinkedList<ICandle>> _candles;

        public CandlesCacheService(int amountOfCandlesToStore, ILog log)
        {
            _amountOfCandlesToStore = amountOfCandlesToStore;
            _log = log;
            _candles = new ConcurrentDictionary<string, LinkedList<ICandle>>();
        }

        public void Initialize(string assetPairId, PriceType priceType, TimeInterval timeInterval, IReadOnlyCollection<ICandle> candles)
        {
            var key = GetKey(assetPairId, priceType, timeInterval);
            var candlesList = new LinkedList<ICandle>(candles.Limit(_amountOfCandlesToStore));

            foreach(var candle in candles)
            {
                if (candle.AssetPairId != assetPairId)
                {
                    throw new ArgumentException($"Candle {candle.ToJson()} has invalid AssetPriceId", nameof(candles));
                }
                if (candle.PriceType != priceType)
                {
                    throw new ArgumentException($"Candle {candle.ToJson()} has invalid PriceType", nameof(candles));
                }
                if (candle.TimeInterval != timeInterval)
                {
                    throw new ArgumentException($"Candle {candle.ToJson()} has invalid TimeInterval", nameof(candles));
                }
            }

            _candles.TryAdd(key, candlesList);
        }

        public void Cache(ICandle candle)
        {
            var key = GetKey(candle.AssetPairId, candle.PriceType, candle.TimeInterval);

            _candles.AddOrUpdate(key,
                addValueFactory: k => AddNewCandlesHistory(candle),
                updateValueFactory: (k, hisotry) => UpdateCandlesHistory(hisotry, candle));
        }

        public IEnumerable<ICandle> GetCandles(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
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

            if (_candles.TryGetValue(key, out LinkedList<ICandle> history))
            {
                IReadOnlyCollection<ICandle> localHistory;
                lock (history)
                {
                    localHistory = history.ToArray();
                }

                // TODO: binnary search could increase speed
                return localHistory
                    .SkipWhile(i => i.Timestamp < fromMoment)
                    .TakeWhile(i => i.Timestamp < toMoment);
            }

            return Enumerable.Empty<ICandle>();
        }

        public IImmutableDictionary<string, IImmutableList<ICandle>> GetState()
        {
            return _candles.ToImmutableDictionary(i => i.Key, i => (IImmutableList<ICandle>)i.Value.ToImmutableList());
        }

        public void SetState(IImmutableDictionary<string, IImmutableList<ICandle>> state)
        {
            if (_candles.Count > 0)
            {
                throw new InvalidOperationException("Cache state can't be set when cache already not empty");
            }

            _candles = new ConcurrentDictionary<string, LinkedList<ICandle>>(
                state.Select(i => KeyValuePair.Create(i.Key, new LinkedList<ICandle>(i.Value))));
        }

        private static LinkedList<ICandle> AddNewCandlesHistory(ICandle candle)
        {
            var history = new LinkedList<ICandle>();
            
            history.AddLast(candle);

            return history;
        }

        private LinkedList<ICandle> UpdateCandlesHistory(LinkedList<ICandle> history, ICandle candle)
        {
            lock (history)
            {
                // Starting from the latest candle, moving down to the history
                for (var node = history.Last; node != null; node = node.Previous)
                {
                    // Candle at given point already exists, so replace it
                    if (node.Value.Timestamp == candle.Timestamp)
                    {
                        node.Value = candle;

                        if (node != history.Last)
                        {
                            _log.WriteWarningAsync(
                                nameof(CandlesCacheService),
                                nameof(UpdateCandlesHistory),
                                candle.ToJson(),
                                $"Cached candle {candle.ToJson()} was not the last one in the cache. It seems that candles was missordered. Last cached candle: {history.Last.Value.ToJson()}");
                        }

                        break;
                    }

                    // If we found more early point than candle,
                    // that's the point after which we should add candle
                    if (node.Value.Timestamp < candle.Timestamp)
                    {
                        var newNode = history.AddAfter(node, candle);

                        // Should we remove oldest candle?
                        if (history.Count > _amountOfCandlesToStore)
                        {
                            history.RemoveFirst();
                        }

                        if (newNode != history.Last)
                        {
                            _log.WriteWarningAsync(
                                nameof(CandlesCacheService),
                                nameof(UpdateCandlesHistory),
                                candle.ToJson(),
                                $"Cached candle {candle.ToJson()} was not the last one in the cache. It seems that quotes was missordered. Last cached candle: {history.Last.Value.ToJson()}");
                        }

                        break;
                    }

                    // If we achieve first node, we should check if cache is full or not
                    if (node == history.First)
                    {
                        if (history.Count < _amountOfCandlesToStore)
                        {
                            // Cache is not full, so we can store the candle as earliest point in the history
                            history.AddBefore(history.First, candle);

                            _log.WriteWarningAsync(
                                nameof(CandlesCacheService),
                                nameof(UpdateCandlesHistory),
                                candle.ToJson(),
                                $"Cached candle {candle.ToJson()} was not the last one in the cache. It seems that quotes was missordered. Last cached candle: {history.Last.Value.ToJson()}");
                        }
                        else
                        {
                            // Cache is full, so we can't store the candle there, because, probably
                            // there is persisted candle at this point

                            _log.WriteWarningAsync(
                                nameof(CandlesCacheService),
                                nameof(UpdateCandlesHistory),
                                candle.ToJson(),
                                $"Can't cache candle it's too old to store in cache. Current history length for {candle.AssetPairId}:{candle.PriceType}:{candle.TimeInterval} = {history.Count}. First cached candle: {history.First.Value.ToJson()}, Last cached candle: {history.Last.Value.ToJson()}");
                        }
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