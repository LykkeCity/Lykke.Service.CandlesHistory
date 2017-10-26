using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Lykke.Domain.Prices;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Core;
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

            var spread = lastCandle.High - lastCandle.Low;

            var result = GenerateCandles(
                    assetPair,
                    priceType,
                    lastCandle.Timestamp,
                    dateTime,
                    lastCandle.Open,
                    lastCandle.Close,
                    spread)
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

                var currentCandleHeight = currentCandle.High - currentCandle.Low;
                var nextCandleHeight = nextCandle.High - nextCandle.Low;
                var spread = (currentCandleHeight + nextCandleHeight) * 0.5;

                var generagedCandles = GenerateCandles(
                    assetPair,
                    currentCandle.PriceType,
                    firstDate,
                    lastDate,
                    currentCandle.Close,
                    nextCandle.Open,
                    spread);

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

        public IEnumerable<ICandle> GenerateCandles(
            IAssetPair assetPair, 
            PriceType priceType, 
            DateTime exclusiveStartDate, 
            DateTime exclusiveEndDate, 
            double startPrice, 
            double endPrice,
            double spread)
        {
            //Console.WriteLine($"Generating missed candles: {exclusiveStartDate} - {exclusiveEndDate}, {startPrice} - {endPrice}");

            var start = exclusiveStartDate.AddSeconds(1);
            var end = exclusiveEndDate.AddSeconds(-1);
            var duration = end - start;
            var prevClose = startPrice;
            var trendSign = startPrice < endPrice ? 1 : -1;

            for (var timestamp = start; timestamp <= end; timestamp = timestamp.AddSeconds(1))
            {
                // Interpolation parameter (0..1)
                var t = (timestamp - start).Seconds / duration.TotalSeconds;
                // Lineary interpolated price for current candle
                var price = MathEx.Lerp(startPrice, endPrice, t);

                var halfSpread = spread * 0.5;
                var min = price - halfSpread;
                var max = price + halfSpread;
                var maxCandleHalfHeight = halfSpread * 0.5;
                
                var mid = _rnd.NextDouble(min, max);
                // Next candle opens at prev candle close and +/- 10% of spread
                var open = prevClose + _rnd.NextDouble(-0.1, 0.1) * spread;

                // in 70% of cases following the trend, and opposite in rest of cases
                var currentSign = _rnd.NextDouble() < 0.7 ? trendSign : -trendSign;

                // Candle can be closed up to 80% of spread from the open price
                var close = open + _rnd.NextDouble(0, 0.8) * spread * currentSign;

                // Preserve candle in the middle of the spread
                if (close > max || close < min)
                {
                    close = mid;
                }

                var high = Math.Max(open, Math.Max(close, mid + _rnd.NextDouble(0, maxCandleHalfHeight)));
                var low = Math.Min(open, Math.Min(close, mid - _rnd.NextDouble(0, maxCandleHalfHeight)));

                //Console.WriteLine($"Generated candle: {timestamp}, {open}, {close}, {low}, {high}");
                
                var newCandle = new Candle(
                    assetPair: assetPair.Id,
                    priceType: priceType,
                    timeInterval: TimeInterval.Sec,
                    timestamp: timestamp,
                    open: Math.Round(open, assetPair.Accuracy),
                    close: Math.Round(close, assetPair.Accuracy),
                    high: Math.Round(high, assetPair.Accuracy),
                    low: Math.Round(low, assetPair.Accuracy));

                prevClose = close;

                yield return newCandle;
            }
        }

        private static string GetKey(string assetPair, PriceType priceType)
        {
            return $"{assetPair}-{priceType}";
        }
    }
}
