using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
using Microsoft.WindowsAzure.Storage.Table;

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
            var partition = FeedBidAskHistoryEntity.GeneratePartitionKey(assetPair);
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition);
            var tableQuery = new TableQuery<FeedBidAskHistoryEntity>().Where(filter);

            return _tableStorage.GetDataByChunksAsync(tableQuery, async chunk =>
            {
                var yieldResult = chunk
                    .SkipWhile(item => item.DateTime < startDate)
                    .TakeWhile(item => item.DateTime <= endDate)
                    .Select(historyItem => historyItem.ToDomain())
                    .ToList();

                if (yieldResult.Count > 0)
                {
                    await chunkCallback(yieldResult);
                }
            });
        }
    }
}
