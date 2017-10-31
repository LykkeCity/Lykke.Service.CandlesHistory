using System;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using MessagePack;

namespace Lykke.Service.CandleHistory.Repositories.Snapshots
{
    [MessagePackObject]
    public class SnapshotCandleEntity : ICandle
    {
        [Key(0)]
        public string AssetPairId { get; set; }

        [Key(1)]
        public PriceType PriceType { get; set; }

        [Key(2)]
        public TimeInterval TimeInterval { get; set; }

        [Key(3)]
        public DateTime Timestamp { get; set; }

        [Key(4)]
        public decimal Open { get; set; }

        [Key(5)]
        public decimal Close { get; set; }

        [Key(6)]
        public decimal High { get; set; }

        [Key(7)]
        public decimal Low { get; set; }

        double ICandle.Open => (double) Open;

        double ICandle.Close => (double) Close;

        double ICandle.High => (double) High;

        double ICandle.Low => (double) Low;

        public static SnapshotCandleEntity Create(ICandle candle)
        {
            return new SnapshotCandleEntity
            {
                AssetPairId = candle.AssetPairId,
                PriceType = candle.PriceType,
                TimeInterval = candle.TimeInterval,
                Timestamp = candle.Timestamp,
                Open = ConvertPrice(candle.Open),
                Close = ConvertPrice(candle.Close),
                Low = ConvertPrice(candle.Low),
                High = ConvertPrice(candle.High)
            };
        }

        private static decimal ConvertPrice(double d)
        {
            try
            {
                return Convert.ToDecimal(d);
            }
            catch (OverflowException)
            {
                return d > 0 ? decimal.MaxValue : decimal.MinValue;
            }
            
        }
    }
}
