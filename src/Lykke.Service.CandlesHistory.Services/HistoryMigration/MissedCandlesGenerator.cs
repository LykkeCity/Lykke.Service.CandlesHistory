using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Common;
using Lykke.Domain.Prices;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
using Lykke.Service.CandlesHistory.Core.Extensions;
using Lykke.Service.CandlesHistory.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    /// <summary>
    /// Generates missed candles for ask and bid sec candles history
    /// </summary>
    public class MissedCandlesGenerator
    {
        private readonly ConcurrentDictionary<string, Candle> _lastCandles;
        private readonly ConcurrentDictionary<string, decimal> _lastNonZeroPrices;
        private readonly Random _rnd;

        public MissedCandlesGenerator()
        {
            _lastCandles = new ConcurrentDictionary<string, Candle>();
            _lastNonZeroPrices = new ConcurrentDictionary<string, decimal>();
            _rnd = new Random();
        }

        public IReadOnlyList<ICandle> FillGapUpTo(IAssetPair assetPair, IFeedHistory feedHistory)
        {
            var key = GetKey(feedHistory.AssetPair, feedHistory.PriceType);
            var candles = feedHistory.Candles
                .Select(item => item.ToCandle(feedHistory.AssetPair, feedHistory.PriceType, feedHistory.DateTime))
                .ToList();

            _lastCandles.TryGetValue(key, out var lastCandle);

            bool removeFirstCandle;

            // Use the last candle as the first candle, if any
            if (lastCandle != null)
            {
                removeFirstCandle = true;
                candles.Insert(0, lastCandle);
            }
            else
            {
                removeFirstCandle = false;
            }
            
            var result = GenerateMissedCandles(assetPair, candles);

            // Remember the last candle, if any
            if (result.Any())
            {
                _lastCandles[key] = Candle.Create(result.Last());
            }

            if (removeFirstCandle)
            {
                result.RemoveAt(0);
            }

            return new ReadOnlyCollection<ICandle>(result);
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

            // Remember the last candle, if any
            if (result.Any())
            {
                _lastCandles[key] = Candle.Create(result.Last());
            }

            return result;
        }

        private IList<ICandle> GenerateMissedCandles(IAssetPair assetPair, IReadOnlyList<ICandle> candles)
        {
            var result = new List<ICandle>();

            if (candles.Any())
            {
                var nextCandle = NormalizeCandlePrices(candles.First());
                
                for (var i = 0; i < candles.Count - 1; i++)
                {
                    var currentCandle = nextCandle;

                    UpdateLastNonZeroPrice(currentCandle);
                    
                    nextCandle = NormalizeCandlePrices(candles[i + 1]);
                    
                    var firstDate = currentCandle.Timestamp;
                    var lastDate = nextCandle.Timestamp;

                    result.Add(currentCandle);

                    var currentCandleHeight = currentCandle.High - currentCandle.Low;
                    var nextCandleHeight = nextCandle.High - nextCandle.Low;
                    var candlesDiff = Math.Abs(nextCandle.Open - currentCandle.Close);
                    var spread = (currentCandleHeight + nextCandleHeight + candlesDiff) / 3;

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

                UpdateLastNonZeroPrice(nextCandle);

                result.Add(nextCandle);
            }

            return result;
        }

        public void RemoveAssetPair(string assetPair)
        {
            foreach (var priceType in Constants.StoredPriceTypes)
            {
                _lastCandles.TryRemove(GetKey(assetPair, priceType), out var _);
            }
        }

        public IEnumerable<ICandle> GenerateCandles(
            IAssetPair assetPair,
            PriceType priceType,
            DateTime exclusiveStartDate,
            DateTime exclusiveEndDate,
            double exclusiveStartPrice,
            double exclusiveEndPrice,
            double spread)
        {
            return GenerateCandles(
                assetPair,
                priceType,
                exclusiveStartDate,
                exclusiveEndDate,
                ConvertToDecimal(exclusiveStartPrice),
                ConvertToDecimal(exclusiveEndPrice),
                ConvertToDecimal(spread));
        }

        private IEnumerable<ICandle> GenerateCandles(
            IAssetPair assetPair, 
            PriceType priceType, 
            DateTime exclusiveStartDate, 
            DateTime exclusiveEndDate, 
            decimal exclusiveStartPrice,
            decimal exclusiveEndPrice,
            decimal spread)
        {
            var start = exclusiveStartDate.AddSeconds(1);
            var end = exclusiveEndDate.AddSeconds(-1);

            if (exclusiveEndDate - exclusiveStartDate <= TimeSpan.FromSeconds(1))
            {
                yield break;
            }

            var duration = (decimal)(exclusiveEndDate - exclusiveStartDate).TotalSeconds;
            var prevClose = exclusiveStartPrice;
            var trendSign = exclusiveStartPrice < exclusiveEndPrice ? 1 : -1;
            // Absolute start to end price change in % of start price
            var totalPriceChange = exclusiveStartPrice != 0m
                ? Math.Abs((exclusiveEndPrice - exclusiveStartPrice) / exclusiveStartPrice)
                : Math.Abs(exclusiveEndPrice - exclusiveStartPrice);
            var stepPriceChange = totalPriceChange / duration;
            var effectiveSpread = spread != 0
                ? Math.Abs(spread)
                : totalPriceChange * 0.2m;

            if (effectiveSpread == 0)
            {
                if (exclusiveStartPrice != 0)
                {
                    effectiveSpread = exclusiveStartPrice * 0.2m;
                }
                else if(exclusiveEndPrice != 0)
                {
                    effectiveSpread = exclusiveEndPrice * 0.2m;
                }
            }

            var backupMid = exclusiveStartPrice;

            if (backupMid == 0)
            {
                backupMid = exclusiveEndPrice != 0 ? exclusiveEndPrice : effectiveSpread;
            }
            
            for (var timestamp = start; timestamp <= end; timestamp = timestamp.AddSeconds(1))
            {
                // Interpolation parameter (0..1)
                var t = (decimal)(timestamp - exclusiveStartDate).TotalSeconds / duration;

                // Lineary interpolated price for current candle
                var mid = MathEx.Lerp(exclusiveStartPrice, exclusiveEndPrice, t);

                if (mid <= 0)
                {
                    mid = backupMid;
                }

                var halfSpread = effectiveSpread * 0.5m;

                // Next candle opens at prev candle close and +/- 5% of spread
                var open = prevClose + _rnd.NextDecimal(-0.05m, 0.05m) * effectiveSpread;

                // in 90% of cases following the trend, and opposite in rest of cases
                var currentSign = _rnd.NextDecimal(0m, 1m) < 0.9m ? trendSign : -trendSign;

                // Candle can be closed up to 200% of price change from the open price
                var close = open + _rnd.NextDecimal(0, 2) * stepPriceChange * currentSign;

                // Lets candles goes up to 500% of the spread in the middle of the generated range, 
                // and only inside the spread at the range boundaries
                var rangeMinMaxDeviationFactor = MathEx.Lerp(1m, 5m, 2m * (0.5m - Math.Abs(0.5m - t)));
                var min = mid - halfSpread * rangeMinMaxDeviationFactor;
                var max = mid + halfSpread * rangeMinMaxDeviationFactor;

                if (min <= 0)
                {
                    min = mid;
                }

                // Returns candles inside 80% of the current range spread, if it gone to far
                if (close > max || close < min)
                {
                    close = mid + _rnd.NextDecimal(-0.8m, 0.8m) * halfSpread * rangeMinMaxDeviationFactor;
                }

                // Max low/high deviation from open/close is 20% of candle height
                var height = Math.Abs(open - close);
                var high = Math.Max(open, close) + _rnd.NextDecimal(0m, 0.1m) * height;
                var low = Math.Min(open, close) - _rnd.NextDecimal(0m, 0.1m) * height;

                if (open <= 0)
                {
                    open = mid;
                }

                if (close <= 0)
                {
                    close = mid;
                }

                if (low <= 0)
                {
                    low = Math.Min(open, close);
                }

                if (high <= 0)
                {
                    high = Math.Max(open, close);
                }

                //Console.WriteLine($"{t:0.000} : {mid:0.000} : {min:0.000}-{max:0.000} : {open:0.000}-{close:0.000} : {low:0.000}-{high:0.000}");

                var newCandle = new Candle(
                    assetPair: assetPair.Id,
                    priceType: priceType,
                    timeInterval: TimeInterval.Sec,
                    timestamp: timestamp,
                    open: (double) Math.Round(open, assetPair.Accuracy),
                    close: (double) Math.Round(close, assetPair.Accuracy),
                    high: (double) Math.Round(high, assetPair.Accuracy),
                    low: (double) Math.Round(low, assetPair.Accuracy),
                    tag: "Randomly generated");

                if (open == 0 || close == 0 || high == 0 || low == 0)
                {
                    var context = new
                    {
                        AssetPair = new
                        {
                            Id = assetPair.Id,
                            Accuracy = assetPair.Accuracy
                        },
                        exclusiveStartDate = exclusiveStartDate,
                        exclusiveEndDate = exclusiveEndDate,
                        start = start,
                        end = end,
                        exclusiveStartPrice = exclusiveStartPrice,
                        exclusiveEndPrice = exclusiveEndPrice,
                        duration = duration,
                        spread = spread,
                        effectiveSpread = effectiveSpread,
                        prevClose = prevClose,
                        trendSign = trendSign,
                        totalPriceChange = totalPriceChange,
                        timestamp = timestamp,
                        t = t,
                        mid = mid,
                        halfSpread = halfSpread,
                        currentSign = currentSign,
                        rangeMinMaxDeviationFactor = rangeMinMaxDeviationFactor,
                        min = min,
                        max = max,
                        height = height
                    };

                    throw new InvalidOperationException($"Generated candle {newCandle.ToJson()} has zero prices. Context: {context.ToJson()}");
                }

                prevClose = close;

                yield return newCandle;
            }
        }

        private ICandle NormalizeCandlePrices(ICandle candle)
        {
            var lastNonZeroPrice = GetLastNonZeroPrice(candle.AssetPairId, candle.PriceType);
            var open = ConvertToDecimal(candle.Open);
            var close = ConvertToDecimal(candle.Close);
            var high = ConvertToDecimal(candle.High);
            var low = ConvertToDecimal(candle.Low);

            if (open == 0 || close == 0 || high == 0 || low == 0)
            {
                open = open == 0 ? lastNonZeroPrice : open;
                close = close == 0 ? lastNonZeroPrice : close;
                high = high == 0 ? lastNonZeroPrice : high;
                low = low == 0 ? lastNonZeroPrice : low;

                return new Candle(candle.AssetPairId, candle.PriceType, candle.TimeInterval, candle.Timestamp, 
                    (double)open,
                    (double)close,
                    (double)high,
                    (double)low,
                    $"Normalized {candle.Tag}");
            }

            return candle;
        }

        private void UpdateLastNonZeroPrice(ICandle candle)
        {
            var key = GetKey(candle.AssetPairId, candle.PriceType);

            var price = ConvertToDecimal(candle.Close);

            if (price == 0m)
            {
                price = ConvertToDecimal(candle.High);

                if (price == 0m)
                {
                    price = ConvertToDecimal(candle.Low);

                    if (price == 0m)
                    {
                        price = ConvertToDecimal(candle.Open);
                    }
                }
            }

            if (price != 0m)
            {
                _lastNonZeroPrices[key] = price;
            }
        }

        private decimal GetLastNonZeroPrice(string assetPair, PriceType priceType)
        {
            var key = GetKey(assetPair, priceType);

            _lastNonZeroPrices.TryGetValue(key, out var price);

            return price;
        }

        private static decimal ConvertToDecimal(double d)
        {
            if (double.IsNaN(d))
            {
                return 0;
            }
            if (double.IsInfinity(d))
            {
                return 0;
            }

            return Convert.ToDecimal(d);
        }

        private static string GetKey(string assetPair, PriceType priceType)
        {
            return $"{assetPair}-{priceType}";
        }
    }
}
