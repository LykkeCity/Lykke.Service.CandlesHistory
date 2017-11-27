using System;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Newtonsoft.Json;

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    public class CandleHistoryItem
    {
        [JsonProperty("O")]
        public double Open { get; private set; }

        [JsonProperty("C")]
        public double Close { get; private set; }

        [JsonProperty("H")]
        public double High { get; private set; }

        [JsonProperty("L")]
        public double Low { get; private set; }

        [JsonProperty("T")]
        public int Tick { get; }

        [JsonProperty("V")]
        public double TradingVolume { get; private set; }

        [JsonProperty("U")]
        public DateTime LastUpdateTimestamp { get; private set; }

        [JsonConstructor]
        public CandleHistoryItem(double open, double close, double high, double low, int tick, double tradingVolume, DateTime lastUpdateTimestamp)
        {
            Open = open;
            Close = close;
            High = high;
            Low = low;
            Tick = tick;
            TradingVolume = tradingVolume;
            LastUpdateTimestamp = lastUpdateTimestamp;
        }

        public ICandle ToCandle(string assetPairId, CandlePriceType priceType, DateTime baseTime, CandleTimeInterval timeInterval)
        {
            return Candle.Create
            (
                open: Open,
                close: Close,
                high: High,
                low: Low,
                assetPair: assetPairId,
                priceType: priceType,
                timeInterval: timeInterval,
                timestamp: baseTime.AddIntervalTicks(Tick, timeInterval),
                tradingVolume: TradingVolume,
                lastUpdateTimestamp: LastUpdateTimestamp
            );
        }

        /// <summary>
        /// Merges candle change with the same asset pair, price type, time interval and timestamp
        /// </summary>
        /// <param name="candleState">Candle state</param>
        public void InplaceMergeWith(ICandle candleState)
        {
            if (LastUpdateTimestamp >= candleState.LastUpdateTimestamp)
            {
                return;
            }

            Close = candleState.Close;
            High = Math.Max(High, candleState.High);
            Low = Math.Min(Low, candleState.Low);
            TradingVolume = candleState.TradingVolume;
            LastUpdateTimestamp = candleState.LastUpdateTimestamp;
        }
    }
}
