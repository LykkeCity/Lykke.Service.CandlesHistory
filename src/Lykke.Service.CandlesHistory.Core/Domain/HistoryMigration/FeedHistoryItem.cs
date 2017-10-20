using System;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration
{
    public class FeedHistoryItem
    {
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public int Tick { get; set; }

        public FeedHistoryItem(){}

        public FeedHistoryItem(double open, double close, double high, double low, int tick)
        {
            Open = open;
            Close = close;
            High = high;
            Low = low;
            Tick = tick;
        }

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
