using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Lykke.Domain.Prices;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
using Lykke.Service.CandlesHistory.Core.Extensions;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    /// <summary>
    /// Generates missed candles for ask and bid sec candles history
    /// </summary>
    public class MissedCandlesGenerator : IHaveState<IImmutableDictionary<string, ICandle>>
    {
        private ConcurrentDictionary<string, Candle> _lastCandles;
        private readonly Random _rnd;

        public MissedCandlesGenerator()
        {
            _lastCandles = new ConcurrentDictionary<string, Candle>();
            _rnd = new Random();
        }

        public IReadOnlyList<ICandle> FillGapUpTo(IAssetPair assetPair, IFeedHistory feedHistory)
        {
            var key = GetKey(feedHistory.AssetPair, feedHistory.PriceType);
            var candles = feedHistory.Candles
                .Select(item => item.ToCandle(feedHistory.AssetPair, feedHistory.PriceType, feedHistory.DateTime, TimeInterval.Sec))
                .ToList();

            _lastCandles.TryGetValue(key, out var lastCandle);

            // Use the last candle as the first candle, if any
            if (lastCandle != null)
            {
                candles.Insert(0, lastCandle);
            }
            
            var result = GenerateMissedCandles(assetPair, candles);

            // Remember the last candle
            _lastCandles[key] = Candle.Create(result.Last());

            return result;
        }

        public IReadOnlyList<ICandle> FillGapUpTo(IAssetPair assetPair, PriceType priceType, DateTime dateTime)
        {
            var key = GetKey(assetPair.Id, priceType);

            _lastCandles.TryGetValue(key, out var lastCandle);

            if (lastCandle == null)
            {
                return new List<ICandle>();
            }

            var result = GenerateCandles(
                    assetPair,
                    priceType,
                    lastCandle.Timestamp,
                    dateTime,
                    lastCandle.Open,
                    lastCandle.Close)
                .ToList();

            // Remember the last candle
            _lastCandles[key] = Candle.Create(result.Last());

            return result;
        }

        private IReadOnlyList<ICandle> GenerateMissedCandles(IAssetPair assetPair, IReadOnlyList<ICandle> candles)
        {
            var result = new List<ICandle>();

            for (var i = 0; i < candles.Count - 1; i++)
            {
                var currentCandle = candles[i];
                var nextCandle = candles[i + 1];

                var firstDate = currentCandle.Timestamp;
                var lastDate = nextCandle.Timestamp;

                result.Add(currentCandle);

                var generagedCandles = GenerateCandles(
                    assetPair,
                    currentCandle.PriceType,
                    firstDate,
                    lastDate,
                    currentCandle.Close,
                    nextCandle.Open);

                result.AddRange(generagedCandles);
            }

            result.Add(candles.Last());

            return result;
        }

        public void RemoveAssetPair(string assetPair)
        {
            foreach (var priceType in Constants.StoredPriceTypes)
            {
                _lastCandles.TryRemove(GetKey(assetPair, priceType), out var _);
            }
        }

        public IImmutableDictionary<string, ICandle> GetState()
        {
            return _lastCandles.ToArray().ToImmutableDictionary(i => i.Key, i => (ICandle)i.Value);

        }

        public void SetState(IImmutableDictionary<string, ICandle> state)
        {
            if (_lastCandles.Count > 0)
            {
                throw new InvalidOperationException("Missed candles generator state already not empty");
            }

            _lastCandles = new ConcurrentDictionary<string, Candle>(state.ToDictionary(
                i => i.Key,
                i => Candle.Create(i.Value)));
        }

        public string DescribeState(IImmutableDictionary<string, ICandle> state)
        {
            return $"Candles count: {state.Count}";
        }

        private IEnumerable<ICandle> GenerateCandles(
            IAssetPair assetPair, 
            PriceType priceType, 
            DateTime exclusiveStartDate, 
            DateTime exclusiveEndDate, 
            double startPrice, 
            double endPrice)
        {
            for (var timestamp = exclusiveStartDate.AddSeconds(1); timestamp < exclusiveEndDate; timestamp = timestamp.AddSeconds(1))
            {
                var min = Math.Min(startPrice, endPrice);
                var max = Math.Max(startPrice, endPrice);
                var halfDeviation = (max - min) / min * 2; // % of mid

                if (halfDeviation < 0.0001)
                {
                    halfDeviation = 0.0001;
                }

                var mid = _rnd.NextDouble(min, max);
                var open = mid + _rnd.NextDouble(-halfDeviation, halfDeviation) * mid;
                var close = mid + _rnd.NextDouble(-halfDeviation, halfDeviation) * mid;
                var lowHigh1 = mid + _rnd.NextDouble(-halfDeviation, halfDeviation) * mid;
                var lowHigh2 = mid + _rnd.NextDouble(-halfDeviation, halfDeviation) * mid;
                var low = Math.Min(lowHigh1, lowHigh2);
                var high = Math.Max(lowHigh1, lowHigh2);

                var newCandle = new Candle(
                    assetPair: assetPair.Id,
                    priceType: priceType,
                    timeInterval: TimeInterval.Sec,
                    timestamp: timestamp,
                    open: Math.Round(open, assetPair.Accuracy),
                    close: Math.Round(close, assetPair.Accuracy),
                    high: Math.Round(high, assetPair.Accuracy),
                    low: Math.Round(low, assetPair.Accuracy));

                yield return newCandle;
            }
        }

        private static string GetKey(string assetPair, PriceType priceType)
        {
            return $"{assetPair}-{priceType}";
        }
    }
}
