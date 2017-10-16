using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public interface IFeedHistory
    {
        string AssetPair { get; }
        PriceType PriceType { get; }
        DateTime DateTime { get; }
        FeedHistoryItem[] Candles { get; }
    }

    public class FeedHistory : IFeedHistory
    {
        public string AssetPair { get; set; }
        public PriceType PriceType { get; set; }
        public DateTime DateTime { get; set; }
        public FeedHistoryItem[] Candles { get; set; }
    }

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

    public interface IFeedHistoryRepository
    {
        Task<IFeedHistory> GetTopRecordAsync(string assetPair);
        Task<IFeedHistory> GetCandle(string assetPair, PriceType priceType, string date);
        Task GetCandlesByChunkAsync(string assetPair, PriceType priceType, DateTime startDate, DateTime endDate, Func<IEnumerable<IFeedHistory>, PriceType, Task> chunkCallback);
    }
}
