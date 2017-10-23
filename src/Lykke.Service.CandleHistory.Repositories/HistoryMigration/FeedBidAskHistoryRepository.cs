using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;

namespace Lykke.Service.CandleHistory.Repositories.HistoryMigration
{
    public class FeedBidAskHistoryRepository : IFeedBidAskHistoryRepository
    {
        private readonly INoSQLTableStorage<FeedBidAskHistoryEntity> _tableStorage;

        public FeedBidAskHistoryRepository(INoSQLTableStorage<FeedBidAskHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task SaveHistoryItemAsync(string assetPair, DateTime date, IEnumerable<ICandle> askCandles, IEnumerable<ICandle> bidCandles)
        {
            await _tableStorage.InsertOrMergeAsync(FeedBidAskHistoryEntity.Create(assetPair, date, askCandles, bidCandles));
        }

        public Task GetHistoryByChunkAsync(string assetPair, DateTime startDate, DateTime endDate, Func<IEnumerable<IFeedBidAskHistory>, Task> chunkCallback)
        {
            return _tableStorage.GetDataByChunksAsync(FeedBidAskHistoryEntity.GeneratePartitionKey(assetPair), async chunk =>
            {
                var yieldResult = new List<IFeedBidAskHistory>();

                foreach (var historyItem in chunk.SkipWhile(item => item.DateTime >= startDate).TakeWhile(item => item.DateTime <= endDate))
                {
                    yieldResult.Add(historyItem.ToDomain());
                }

                if (yieldResult.Count > 0)
                {
                    await chunkCallback(yieldResult);
                    yieldResult.Clear();
                }
            });
        }
    }
}
