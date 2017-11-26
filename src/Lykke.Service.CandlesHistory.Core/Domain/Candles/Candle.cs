using System;
using Lykke.Job.CandlesProducer.Contract;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public class Candle : ICandle
    {
        public string AssetPairId { get; }
        public CandlePriceType PriceType { get; }
        public CandleTimeInterval TimeInterval { get; }
        public DateTime Timestamp { get; }
        public double Open { get; }
        public double Close { get; }
        public double High { get; }
        public double Low { get; }

        public Candle(string assetPair, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime timestamp, double open, double close, double high, double low)
        {
            AssetPairId = assetPair;
            PriceType = priceType;
            TimeInterval = timeInterval;
            Timestamp = timestamp;
            Open = open;
            Close = close;
            High = high;
            Low = low;
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
                low: candle.Low
            );
        }
    }
}
