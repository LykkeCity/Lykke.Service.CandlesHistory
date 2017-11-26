using System;
using Lykke.Job.CandlesProducer.Contract;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration.HistoryProviders.MeFeedHistory
{
    public interface IFeedHistory
    {
        string AssetPair { get; }
        CandlePriceType PriceType { get; }
        DateTime DateTime { get; }
        FeedHistoryItem[] Candles { get; }
    }
}
