using System;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration
{
    public interface IFeedHistory
    {
        string AssetPair { get; }
        PriceType PriceType { get; }
        DateTime DateTime { get; }
        FeedHistoryItem[] Candles { get; }
    }
}
