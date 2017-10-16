using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public interface IFeedBidAskHistory
    {
        string AssetPair { get; }
        DateTime DateTime { get; }
        ICandle[] BidCandles { get; }
        ICandle[] AskCandles { get; }
    }

    public class FeedBidAskHistory : IFeedBidAskHistory
    {
        public string AssetPair { get; set; }
        public DateTime DateTime { get; set; }
        public ICandle[] BidCandles { get; set; }
        public ICandle[] AskCandles { get; set; }
    }

    public interface IFeedBidAskHistoryRepository
    {
        Task AddHistoryItemAsync(string assetPair, DateTime date, List<ICandle> askCandles, List<ICandle> bidCandles);
        Task GetHistoryByChunkAsync(string assetPair, DateTime startDate, DateTime endDate, Func<IEnumerable<IFeedBidAskHistory>, Task> chunkCallback);
    }
}
