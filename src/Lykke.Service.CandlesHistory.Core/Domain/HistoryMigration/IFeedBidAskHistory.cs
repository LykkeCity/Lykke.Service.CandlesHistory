using System;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration
{
    public interface IFeedBidAskHistory
    {
        string AssetPair { get; }
        DateTime DateTime { get; }
        ICandle[] BidCandles { get; }
        ICandle[] AskCandles { get; }
    }
}
