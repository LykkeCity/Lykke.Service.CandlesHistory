using System;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Newtonsoft.Json;

namespace Lykke.Service.CandleHistory.Repositories.Snapshots.Legacy
{
    [Obsolete("Used for snapshot migration")]
    public class LegacySnapshotCandleEntity : ICandle
    {
        [JsonProperty("a")]
        public string AssetPairId { get; set; }

        [JsonProperty("p")]
        public PriceType PriceType { get; set; }

        [JsonProperty("i")]
        public TimeInterval TimeInterval { get; set; }

        [JsonProperty("t")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("o")]
        public double Open { get; set; }

        [JsonProperty("c")]
        public double Close { get; set; }

        [JsonProperty("h")]
        public double High { get; set; }

        [JsonProperty("l")]
        public double Low { get; set; }

        public static LegacySnapshotCandleEntity Create(ICandle candle)
        {
            return new LegacySnapshotCandleEntity
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
    }
}
