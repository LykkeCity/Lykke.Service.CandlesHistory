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
        public double TradingVolume { get; }
        public double TradingOppositeVolume { get; }
        public DateTime LastUpdateTimestamp { get; }

        private Candle(
            string assetPair, 
            CandlePriceType priceType, 
            CandleTimeInterval timeInterval,
            DateTime timestamp, 
            double open, 
            double close,
            double high, 
            double low,
            double tradingVolume,
            double tradingOppositeVolume,
            DateTime lastUpdateTimestamp)
        {
            AssetPairId = assetPair;
            PriceType = priceType;
            TimeInterval = timeInterval;
            Timestamp = timestamp;
            Open = open;
            Close = close;
            High = high;
            Low = low;
            TradingVolume = tradingVolume;
            TradingOppositeVolume = tradingOppositeVolume;
            LastUpdateTimestamp = lastUpdateTimestamp;
        }

        public static Candle Create(
            string assetPair,
            CandlePriceType priceType, 
            CandleTimeInterval timeInterval,
            DateTime timestamp, 
            double open, 
            double close, 
            double high, 
            double low, 
            double tradingVolume,
            double tradingOppositeVolume,
            DateTime lastUpdateTimestamp)
        {
            return new Candle(
                assetPair,
                priceType,
                timeInterval,
                timestamp.TruncateTo(timeInterval),
                open,
                close,
                high,
                low,
                tradingVolume,
                tradingOppositeVolume,
                lastUpdateTimestamp);
        }
    }
}
