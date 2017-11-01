using System;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Newtonsoft.Json;

namespace Lykke.Service.CandleHistory.Repositories.Candles
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

        [JsonProperty("Tag")]
        public string Tag { get; set; }

        public ICandle ToCandle(string assetPairId, PriceType priceType, DateTime baseTime, TimeInterval timeInterval)
        {
            return new Candle(
                open: Open,
                close: Close,
                high: High,
                low: Low,
                assetPair: assetPairId,
                priceType: priceType,
                timeInterval: timeInterval,
                timestamp: baseTime.AddIntervalTicks(Tick, timeInterval),
                tag: Tag);
        }

        /// <summary>
        /// Merges candle change with the same asset pair, price type, time interval and timestamp
        /// </summary>
        /// <param name="candleChange">Candle change</param>
        public void InplaceMergeWith(ICandle candleChange)
        {
            Close = candleChange.Close;
            High = Math.Max(High, candleChange.High);
            Low = Math.Min(Low, candleChange.Low);
        }
    }
}
