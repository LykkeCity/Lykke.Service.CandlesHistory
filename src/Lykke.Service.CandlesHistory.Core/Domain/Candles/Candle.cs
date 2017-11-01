using System;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public class Candle : ICandle
    {
        public string AssetPairId { get; }
        public PriceType PriceType { get; }
        public TimeInterval TimeInterval { get; }
        public DateTime Timestamp { get; }
        public double Open { get; }
        public double Close { get; }
        public double High { get; }
        public double Low { get; }
        public string Tag { get; }

        public Candle(string assetPair, PriceType priceType, TimeInterval timeInterval, DateTime timestamp, double open, double close, double high, double low, string tag)
        {
            AssetPairId = assetPair;
            PriceType = priceType;
            TimeInterval = timeInterval;
            Timestamp = timestamp;
            Open = open;
            Close = close;
            High = high;
            Low = low;
            Tag = tag;
        }

        public static Candle Create(ICandle candle)
        {
            return new Candle(

                assetPair: candle.AssetPairId,
                priceType: candle.PriceType,
                timeInterval: candle.TimeInterval,
                timestamp: candle.Timestamp,
                open: candle.Open,
                close: candle.Close,
                high: candle.High,
                low: candle.Low,
                tag: candle.Tag
            );
        }
    }
}
