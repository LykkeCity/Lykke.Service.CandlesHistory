using System;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Domain
{
    public class MigrationCandle : IEquatable<MigrationCandle>, ICandle
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

        private MigrationCandle(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime timestamp, double open, double close, double low, double high, double tradingVolume)
        {
            AssetPairId = assetPairId;
            PriceType = priceType;
            TimeInterval = timeInterval;
            Timestamp = timestamp;
            Open = open;
            Close = close;
            Low = low;
            High = high;
            TradingVolume = tradingVolume;
        }

        public static MigrationCandle Create(ICandle candle, CandleTimeInterval? interval = null)
        {
            var intervalTimestamp = interval.HasValue ? candle.Timestamp.TruncateTo(interval.Value) : candle.Timestamp;

            return new MigrationCandle
            (
                candle.AssetPairId,
                candle.PriceType,
                interval ?? candle.TimeInterval,
                intervalTimestamp,
                candle.Open,
                candle.Close,
                candle.Low,
                candle.High,
                candle.TradingVolume
            );
        }

        public MigrationCandle Merge(ICandle otherCandle)
        {
            var intervalTimestamp = otherCandle.Timestamp.TruncateTo(TimeInterval);

            // Start new candle?
            if (intervalTimestamp != Timestamp)
                return Create(otherCandle, TimeInterval);

            // Merge candles

            return new MigrationCandle
            (
                AssetPairId,
                PriceType,
                TimeInterval,
                Timestamp,
                Open,
                otherCandle.Close,
                Math.Min(Low, otherCandle.Low),
                Math.Max(High, otherCandle.High),
                TradingVolume + otherCandle.TradingVolume
            );
        }

        public bool Equals(MigrationCandle other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return string.Equals(AssetPairId, other.AssetPairId) &&
                   PriceType == other.PriceType &&
                   TimeInterval == other.TimeInterval &&
                   Timestamp.Equals(other.Timestamp) &&
                   Open.Equals(other.Open) &&
                   Close.Equals(other.Close) &&
                   High.Equals(other.High) &&
                   Low.Equals(other.Low) &&
                   TradingVolume.Equals(other.TradingVolume);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((MigrationCandle)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AssetPairId != null ? AssetPairId.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (int)PriceType;
                hashCode = (hashCode * 397) ^ (int)TimeInterval;
                hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ Open.GetHashCode();
                hashCode = (hashCode * 397) ^ Close.GetHashCode();
                hashCode = (hashCode * 397) ^ High.GetHashCode();
                hashCode = (hashCode * 397) ^ Low.GetHashCode();
                hashCode = (hashCode * 397) ^ TradingVolume.GetHashCode();

                return hashCode;
            }
        }
    }
}
