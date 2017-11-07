using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Extensions
{
    public static class CandleExtensions
    {
        /// <summary>
        /// Merges all of candles placed in chronological order
        /// </summary>
        /// <param name="candles">Candles in hronological order</param>
        /// <param name="newTimestamp">
        /// <see cref="ICandle.Timestamp"/> of merged candle, if not specified, 
        /// then <see cref="ICandle.Timestamp"/> of all candles should be equals, 
        /// and it will be used as merged candle <see cref="ICandle.Timestamp"/>
        /// </param>
        /// <returns>Merged candle, or null, if no candles to merge</returns>
        private static ICandle MergeAll(this IEnumerable<ICandle> candles, DateTime? newTimestamp = null)
        {
            if (candles == null)
            {
                return null;
            }

            var open = 0d;
            var close = 0d;
            var high = 0d;
            var low = 0d;
            var assetPairId = string.Empty;
            var priceType = PriceType.Unspecified;
            var timeInterval = TimeInterval.Unspecified;
            var timestamp = DateTime.MinValue;
            var count = 0;

            using (var enumerator = candles.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var candle = enumerator.Current;

                    if (count == 0)
                    {
                        open = candle.Open;
                        close = candle.Close;
                        high = candle.High;
                        low = candle.Low;
                        assetPairId = candle.AssetPairId;
                        priceType = candle.PriceType;
                        timeInterval = candle.TimeInterval;
                        timestamp = candle.Timestamp;
                    }
                    else
                    {
                        if (assetPairId != candle.AssetPairId)
                        {
                            throw new InvalidOperationException($"Can't merge candles of different asset pairs. Current candle={candle.ToJson()}");
                        }

                        if (priceType != candle.PriceType)
                        {
                            throw new InvalidOperationException($"Can't merge candles of different price types. Current candle={candle.ToJson()}");
                        }

                        if (timeInterval != candle.TimeInterval)
                        {
                            throw new InvalidOperationException($"Can't merge candles of different time intervals. Current candle={candle.ToJson()}");
                        }

                        if (!newTimestamp.HasValue && timestamp != candle.Timestamp)
                        {
                            throw new InvalidOperationException($"Can't merge candles with different timestamps. Current candle={candle.ToJson()}");
                        }

                        close = candle.Close;
                        high = Math.Max(high, candle.High);
                        low = Math.Min(low, candle.Low);
                    }

                    count++;
                }
            }

            if (count > 0)
            {
                return new Candle(
                    open: open,
                    close: close,
                    high: high,
                    low: low,
                    assetPair: assetPairId,
                    priceType: priceType,
                    timeInterval: timeInterval,
                    timestamp: newTimestamp ?? timestamp);
            }

            return null;
        }

        /// <summary>
        /// Merges candles into bigger intervals (e.g. Minute -> Min15).
        /// </summary>
        /// <param name="candles">Candles to merge</param>
        /// <param name="newInterval">New interval</param>
        public static IEnumerable<ICandle> MergeIntoBiggerIntervals(this IEnumerable<ICandle> candles, TimeInterval newInterval)
        {
            return candles
                .GroupBy(c => c.Timestamp.RoundTo(newInterval))
                .Select(g => g.MergeAll(g.Key));
        }

        /// <summary>
        /// Creates mid candle of two candles (ask and bid)
        /// </summary>
        /// <param name="askCandle">first candle</param>
        /// <param name="bidCandle">second candle</param>
        public static ICandle CreateMidCandle(this ICandle askCandle, ICandle bidCandle)
        {
            if (askCandle == null || bidCandle == null)
            {
                return askCandle ?? bidCandle;
            }

            if (askCandle.AssetPairId != bidCandle.AssetPairId)
            {
                throw new InvalidOperationException($"Can't create mid candle of different asset pairs. candle1={askCandle.ToJson()}, candle2={bidCandle.ToJson()}");
            }

            if (askCandle.PriceType != PriceType.Ask)
            {
                throw new InvalidOperationException($"Ask candle should has according price type. candle={askCandle.ToJson()}");
            }

            if (bidCandle.PriceType != PriceType.Bid)
            {
                throw new InvalidOperationException($"Bid candle should has according price type. candle={bidCandle.ToJson()}");
            }

            if (askCandle.TimeInterval != bidCandle.TimeInterval)
            {
                throw new InvalidOperationException($"Can't create mid candle of different time intervals. candle1={askCandle.ToJson()}, candle2={bidCandle.ToJson()}");
            }

            if (askCandle.Timestamp != bidCandle.Timestamp)
            {
                throw new InvalidOperationException($"Can't create mid candle from candles with different timestamps. candle1={askCandle.ToJson()}, candle2={bidCandle.ToJson()}");
            }

            return new Candle(
                open: (askCandle.Open + bidCandle.Open) / 2,
                close: (askCandle.Close + bidCandle.Close) / 2,
                high: (askCandle.High + bidCandle.High) / 2,
                low: (askCandle.Low + bidCandle.Low) / 2,
                assetPair: askCandle.AssetPairId,
                priceType: PriceType.Mid,
                timeInterval: askCandle.TimeInterval,
                timestamp: askCandle.Timestamp);
        }
    }
}
