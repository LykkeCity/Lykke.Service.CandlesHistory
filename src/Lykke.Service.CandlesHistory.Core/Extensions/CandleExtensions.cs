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
        /// Merges two candles placed in chronological order
        /// </summary>
        /// <param name="prevCandle">Previous candle</param>
        /// <param name="nextCandle">Next candle</param>
        /// <param name="newTimestamp">
        /// <see cref="ICandle.Timestamp"/> of merged candle, if not specified, 
        /// then <see cref="ICandle.Timestamp"/> of both candles should be equal, 
        /// and it will be used as merged candle <see cref="ICandle.Timestamp"/>
        /// </param>
        public static ICandle MergeWith(this ICandle prevCandle, ICandle nextCandle, DateTime? newTimestamp = null)
        {
            if (prevCandle == null || nextCandle == null)
            {
                return prevCandle ?? nextCandle;
            }

            if (prevCandle.AssetPairId != nextCandle.AssetPairId)
            {
                throw new InvalidOperationException($"Can't merge candles of different asset pairs. Source={prevCandle.ToJson()}, update={nextCandle.ToJson()}");
            }

            if (prevCandle.PriceType != nextCandle.PriceType)
            {
                throw new InvalidOperationException($"Can't merge candles of different price types. Source={prevCandle.ToJson()}, update={nextCandle.ToJson()}");
            }

            if (prevCandle.TimeInterval != nextCandle.TimeInterval)
            {
                throw new InvalidOperationException($"Can't merge candles of different time intervals. Source={prevCandle.ToJson()}, update={nextCandle.ToJson()}");
            }

            if (!newTimestamp.HasValue && prevCandle.Timestamp != nextCandle.Timestamp)
            {
                throw new InvalidOperationException($"Can't merge candles with different timestamps. Source={prevCandle.ToJson()}, update={nextCandle.ToJson()}");
            }

            return new Candle
            {
                Open = prevCandle.Open,
                Close = nextCandle.Close,
                High = Math.Max(prevCandle.High, nextCandle.High),
                Low = Math.Min(prevCandle.Low, nextCandle.Low),
                AssetPairId = prevCandle.AssetPairId,
                PriceType = prevCandle.PriceType,
                TimeInterval = prevCandle.TimeInterval,
                Timestamp = newTimestamp ?? prevCandle.Timestamp
            };
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

            return new Candle
            {
                Open = (askCandle.Open + bidCandle.Open) / 2,
                Close = (askCandle.Close + bidCandle.Close) / 2,
                High = (askCandle.High + bidCandle.High) / 2,
                Low = (askCandle.Low + bidCandle.Low) / 2,
                AssetPairId = askCandle.AssetPairId,
                PriceType = PriceType.Mid,
                TimeInterval = askCandle.TimeInterval,
                Timestamp = askCandle.Timestamp
            };
        }

        public static List<ICandle> CreateMidCandles(this ICandle[] askCandles, ICandle[] bidCandles)
        {
            if (askCandles.Length != bidCandles.Length)
            {
                throw new InvalidOperationException($"Candles count must be the same for bid and ask candles: ask candles count={askCandles.Length}, bid candles count={bidCandles.Length}");
            }

            return askCandles.Select((candle, index) => candle.CreateMidCandle(bidCandles[index])).ToList();
        }
    }
}
