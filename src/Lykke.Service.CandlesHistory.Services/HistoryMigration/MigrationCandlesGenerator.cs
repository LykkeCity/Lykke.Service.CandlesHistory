using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    public class MigrationCandlesGenerator : IHaveState<IImmutableDictionary<string, ICandle>>
    {
        public class Candle : IEquatable<Candle>, ICandle
        {
            public string AssetPairId { get; }
            public PriceType PriceType { get;}
            public TimeInterval TimeInterval { get; }
            public DateTime Timestamp { get; }
            public double Open { get; }
            public double Close { get; }
            public double High { get; }
            public double Low { get; }

            private Candle(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime timestamp, double open, double close, double low, double high)
            {
                AssetPairId = assetPairId;
                PriceType = priceType;
                TimeInterval = timeInterval;
                Timestamp = timestamp;
                Open = open;
                Close = close;
                Low = low;
                High = high;
            }

            public static Candle Create(ICandle candle, TimeInterval? interval = null)
            {
                var intervalTimestamp = interval.HasValue ? candle.Timestamp.RoundTo(interval.Value) : candle.Timestamp;

                return new Candle
                (
                    candle.AssetPairId,
                    candle.PriceType,
                    interval ?? candle.TimeInterval,
                    intervalTimestamp,
                    candle.Open,
                    candle.Close,
                    candle.Low,
                    candle.High
                );
            }

            public static Candle Create(ICandle oldCandle, ICandle newCandle)
            {
                var intervalTimestamp = newCandle.Timestamp.RoundTo(oldCandle.TimeInterval);

                // Start new candle?
                if (intervalTimestamp != oldCandle.Timestamp)
                    return Create(newCandle, oldCandle.TimeInterval);

                // Merge candles

                return new Candle
                (
                    oldCandle.AssetPairId,
                    oldCandle.PriceType,
                    oldCandle.TimeInterval,
                    oldCandle.Timestamp,
                    oldCandle.Open,
                    newCandle.Close,
                    Math.Min(oldCandle.Low, newCandle.Low),
                    Math.Max(oldCandle.High, newCandle.High)
                );
            }

            public bool Equals(Candle other)
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
                       Low.Equals(other.Low);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;

                if (ReferenceEquals(this, obj))
                    return true;

                if (obj.GetType() != GetType())
                    return false;

                return Equals((Candle)obj);
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

                    return hashCode;
                }
            }
        }

        private Dictionary<string, Candle> _candles;

        public MigrationCandlesGenerator()
        {
            _candles = new Dictionary<string, Candle>();
        }

        public MigrationCandleMergeResult Merge(ICandle candle, TimeInterval timeInterval)
        {
            var key = GetKey(timeInterval, candle.PriceType);
            var newCandle = _candles.TryGetValue(key, out var oldCandle) ?
                Candle.Create(oldCandle, candle) :
                Candle.Create(candle, timeInterval);

            _candles[key] = newCandle;

            return new MigrationCandleMergeResult(oldCandle, oldCandle != null && !newCandle.Timestamp.Equals(oldCandle.Timestamp));
        }

        private static string GetKey(TimeInterval timeInterval, PriceType type)
        {
            return $"{type}-{timeInterval}";
        }

        public IImmutableDictionary<string, ICandle> GetState()
        {
            return _candles.ToImmutableDictionary(i => i.Key, i => (ICandle)i.Value);
        }

        public void SetState(IImmutableDictionary<string, ICandle> state)
        {
            if (_candles.Count > 0)
            {
                throw new InvalidOperationException("Candles generator state already not empty");
            }

            _candles = state.ToDictionary(i => i.Key, i => Candle.Create(i.Value));
        }

        public string DescribeState(IImmutableDictionary<string, ICandle> state)
        {
            return $"Candles count: {state.Count}";
        }

    }
}
