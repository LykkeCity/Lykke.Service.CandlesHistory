using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration.HistoryProviders.MeFeedHistory
{
    public interface IFeedHistoryRepository
    {
        Task<IFeedHistory> GetTopRecordAsync(string assetPair, PriceType priceType);
        Task GetCandlesByChunksAsync(string assetPair, PriceType priceType, DateTime endDate, Func<IEnumerable<IFeedHistory>, Task> readChunkFunc);
    }
}
