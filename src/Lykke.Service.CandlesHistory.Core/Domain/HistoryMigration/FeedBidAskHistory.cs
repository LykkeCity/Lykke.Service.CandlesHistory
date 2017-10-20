using System;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration
{
    public class FeedBidAskHistory : IFeedBidAskHistory
    {
        public string AssetPair { get; set; }
        public DateTime DateTime { get; set; }
        public ICandle[] BidCandles { get; set; }
        public ICandle[] AskCandles { get; set; }
    }
}
