using System;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration
{
    public class FeedHistory : IFeedHistory
    {
        public string AssetPair { get; set; }
        public PriceType PriceType { get; set; }
        public DateTime DateTime { get; set; }
        public FeedHistoryItem[] Candles { get; set; }
    }
}
