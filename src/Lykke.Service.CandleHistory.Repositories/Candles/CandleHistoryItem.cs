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

        [JsonConstructor]
        public CandleHistoryItem(double open, double close, double high, double low, int tick, double tradingVolume)
        {
            Open = open;
            Close = close;
            High = high;
            Low = low;
            Tick = tick;
            TradingVolume = tradingVolume;
        }

        public ICandle ToCandle(string assetPairId, CandlePriceType priceType, DateTime baseTime, CandleTimeInterval timeInterval)
        {
            return new Candle
            (
                open: Open,
                close: Close,
                high: High,
                low: Low,
                assetPair: assetPairId,
                priceType: priceType,
                timeInterval: timeInterval,
                timestamp: baseTime.AddIntervalTicks(Tick, timeInterval),
                tradingVolume: TradingVolume
            );
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
            TradingVolume += candleChange.TradingVolume;
        }
    }
}
