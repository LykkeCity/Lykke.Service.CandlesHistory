using System;
using Common;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandleHistory.Repositories
{
    internal static class CandleExtensions
    {
        public static CandleHistoryItem ToItem(this ICandle candle, TimeInterval interval)
        {
            return new CandleHistoryItem
            {
                Open = candle.Open,
                Close = candle.Close,
                High = candle.High,
                Low = candle.Low,
                Tick = candle.Timestamp.GetIntervalTick(interval)
            };
        }

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
    }
}