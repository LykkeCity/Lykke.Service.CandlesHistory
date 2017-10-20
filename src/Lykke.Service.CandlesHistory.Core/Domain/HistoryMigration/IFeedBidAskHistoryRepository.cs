using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration
{
    public interface IFeedBidAskHistoryRepository
    {
        Task SaveHistoryItemAsync(string assetPair, DateTime date, List<ICandle> askCandles, List<ICandle> bidCandles);
        Task GetHistoryByChunkAsync(string assetPair, DateTime startDate, DateTime endDate, Func<IEnumerable<IFeedBidAskHistory>, Task> chunkCallback);
    }
}
