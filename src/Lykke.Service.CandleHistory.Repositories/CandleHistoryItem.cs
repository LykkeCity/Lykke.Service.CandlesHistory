using System;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Newtonsoft.Json;

namespace Lykke.Service.CandleHistory.Repositories
{
    public class CandleHistoryItem
    {
        [JsonProperty("O")]
        public double Open { get; internal set; }

        [JsonProperty("C")]
        public double Close { get; internal set; }

        [JsonProperty("H")]
        public double High { get; internal set; }

        [JsonProperty("L")]
        public double Low { get; internal set; }

        [JsonProperty("T")]
        public int Tick { get; set; }

        public ICandle ToCandle(string assetPairId, PriceType priceType, DateTime baseTime, TimeInterval timeInterval)
        {
            return new Candle
            {
                Open = Open,
                Close = Close,
                High = High,
                Low = Low,
                AssetPairId = assetPairId,
                PriceType = priceType,
                TimeInterval = timeInterval,
                Timestamp = baseTime.AddIntervalTicks(Tick, timeInterval)
            };
        }
    }
}