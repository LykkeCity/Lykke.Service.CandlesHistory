using System;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Extensions;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public class Candle : ICandle
    {
        public string AssetPairId { get; set; }
        public PriceType PriceType { get; set; }
        public TimeInterval TimeInterval { get; set; }
        public DateTime Timestamp { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }

        public static Candle Create(ICandle candle)
        {
            return new Candle
            {
                AssetPairId = candle.AssetPairId,
                PriceType = candle.PriceType,
                TimeInterval = candle.TimeInterval,
                Timestamp = candle.Timestamp,
                Open = candle.Open,
                Close = candle.Close,
                Low = candle.Low,
                High = candle.High
            };
        }

        public static Candle Create(ICandle candle, TimeInterval interval)
        {
            return new Candle
            {
                AssetPairId = candle.AssetPairId,
                PriceType = candle.PriceType,
                TimeInterval = interval,
                Timestamp = candle.Timestamp,
                Open = candle.Open,
                Close = candle.Close,
                Low = candle.Low,
                High = candle.High
            };
        }

        public static Candle Create(ICandle oldCandle, ICandle newCandle)
        {
            var intervalTimestamp = newCandle.Timestamp.RoundTo(oldCandle.TimeInterval);

            // Start new candle?
            if (intervalTimestamp != oldCandle.Timestamp)
                return Create(newCandle);

            // Merge candles
            return Create(oldCandle.MergeWith(newCandle));
        }
    }
}
