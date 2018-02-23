using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class InMemoryCandlesCacheService : ICandlesCacheService
    {
        private readonly int _amountOfCandlesToStore;
        private readonly ILog _log;

        /// <summary>
        /// Candles keyed by asset ID, price type and time interval type are sorted by DateTime 
        /// </summary>
        private ConcurrentDictionary<string, LinkedList<ICandle>> _candles;

        public InMemoryCandlesCacheService(int amountOfCandlesToStore, ILog log)
        {
            _amountOfCandlesToStore = amountOfCandlesToStore;
            _log = log;
            _candles = new ConcurrentDictionary<string, LinkedList<ICandle>>();
        }

        public Task InitializeAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, IReadOnlyCollection<ICandle> candles)
        {
            var key = GetKey(assetPairId, priceType, timeInterval);
            var candlesList = new LinkedList<ICandle>(candles.Take(_amountOfCandlesToStore));

            foreach (var candle in candlesList)
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

            if (candlesList.Any())
            {
                _candles.TryAdd(key, candlesList);
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
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

            if (_candles.TryGetValue(key, out var history))
            {
                IReadOnlyCollection<ICandle> localHistory;
                lock (history)
                {
                    localHistory = history.ToArray();
                }

                // TODO: binnary search could increase speed

                var result = localHistory
                    .SkipWhile(i => i.Timestamp < fromMoment)
                    .TakeWhile(i => i.Timestamp < toMoment);

                return Task.FromResult(result);
            }

            return Task.FromResult(Enumerable.Empty<ICandle>());
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

            var pairs = state
                .Where(i => i.Value.Any())
                .Select(i => KeyValuePair.Create(i.Key, new LinkedList<ICandle>(i.Value)));

            _candles = new ConcurrentDictionary<string, LinkedList<ICandle>>(pairs);
        }

        public string DescribeState(IImmutableDictionary<string, IImmutableList<ICandle>> state)
        {
            return $"Assets: {state.Count}, Total Candles: {state.Values.Sum(list => list.Count)}";
        }

        private static string GetKey(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval)
        {
            return $"{assetPairId.Trim().ToUpper()}-{priceType}-{timeInterval}";
        }
    }
}
