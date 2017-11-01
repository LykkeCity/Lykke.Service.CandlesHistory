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

        public ICandle ToCandle(string assetPairId, PriceType priceType, DateTime baseTime)
        {
            return new Candle(
                open: Open,
                close: Close,
                high: High,
                low: Low,
                assetPair: assetPairId,
                priceType: priceType,
                timeInterval: TimeInterval.Sec,
                timestamp: baseTime.AddIntervalTicks(Tick, TimeInterval.Sec),
                tag: "Feed History");
        }
    }
}
