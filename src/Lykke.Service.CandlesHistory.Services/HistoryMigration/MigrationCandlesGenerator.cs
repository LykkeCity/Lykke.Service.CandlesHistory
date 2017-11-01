using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Services.Candles;

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
            public string Tag { get; }

            private Candle(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime timestamp, double open, double close, double low, double high, string tag)
            {
                AssetPairId = assetPairId;
                PriceType = priceType;
                TimeInterval = timeInterval;
                Timestamp = timestamp;
                Open = open;
                Close = close;
                Low = low;
                High = high;
                Tag = tag;
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
                    candle.High,
                    candle.Tag
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
                    Math.Max(oldCandle.High, newCandle.High),
                    newCandle.Tag
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

        private ConcurrentDictionary<string, Candle> _candles;

        public MigrationCandlesGenerator()
        {
            _candles = new ConcurrentDictionary<string, Candle>();
        }

        public MigrationCandleMergeResult Merge(ICandle candle, TimeInterval timeInterval)
        {
            var key = GetKey(candle.AssetPairId, timeInterval, candle.PriceType);

            Candle oldCandle = null;
            var newCandle = _candles.AddOrUpdate(key,
                addValueFactory: k => Candle.Create(candle, timeInterval),
                updateValueFactory: (k, old) =>
                {
                    oldCandle = old;
                    return Candle.Create(oldCandle, candle);
                });

            return new MigrationCandleMergeResult(newCandle, !newCandle.Equals(oldCandle));
        }

        private static string GetKey(string assetPair, TimeInterval timeInterval, PriceType type)
        {
            return $"{assetPair}-{type}-{timeInterval}";
        }

        public IImmutableDictionary<string, ICandle> GetState()
        {
            return _candles.ToArray().ToImmutableDictionary(i => i.Key, i => (ICandle)i.Value);
        }

        public void SetState(IImmutableDictionary<string, ICandle> state)
        {
            if (_candles.Count > 0)
            {
                throw new InvalidOperationException("Candles generator state already not empty");
            }

            _candles = new ConcurrentDictionary<string, Candle>(state.ToDictionary(
                i => i.Key,
                i => Candle.Create(i.Value)));
        }

        public string DescribeState(IImmutableDictionary<string, ICandle> state)
        {
            return $"Candles count: {state.Count}";
        }

        public void RemoveAssetPair(string assetPair)
        {
            foreach (var priceType in Constants.StoredPriceTypes)
            {
                foreach (var timeInterval in Constants.StoredIntervals)
                {
                    _candles.TryRemove(GetKey(assetPair, timeInterval, priceType), out var _);
                }
            }
        }
    }
}
