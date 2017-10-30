using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.CandleHistory.Repositories.HistoryMigration
{
    public class FeedHistoryRepository : IFeedHistoryRepository
    {
        private readonly INoSQLTableStorage<FeedHistoryEntity> _tableStorage;

        public FeedHistoryRepository(INoSQLTableStorage<FeedHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IFeedHistory> GetTopRecordAsync(string assetPair, PriceType priceType)
        {
            return await _tableStorage.GetTopRecordAsync($"{assetPair}_{priceType}");
        }

        public async Task<IFeedHistory> GetCandle(string assetPair, PriceType priceType, string date)
        {
            return await _tableStorage.GetDataAsync(FeedHistoryEntity.GeneratePartitionKey(assetPair, priceType), date);
        }

        public Task GetCandlesByChunkAsync(string assetPair, PriceType priceType, DateTime startDate, DateTime endDate, Func<IEnumerable<IFeedHistory>, PriceType, Task> chunkCallback)
        {
            var partition = FeedHistoryEntity.GeneratePartitionKey(assetPair, priceType);
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition);
            var tableQuery = new TableQuery<FeedHistoryEntity>().Where(filter);
            var feedStartDate = startDate.RoundToMinute();
            var feedEndDate = endDate.RoundToMinute();

            return _tableStorage.GetDataByChunksAsync(tableQuery, async chunk =>
            {
                var yieldResult = new List<IFeedHistory>();

                foreach (var historyItem in chunk.Where(item => item.DateTime >= feedStartDate && item.DateTime <= feedEndDate))
                {
                    yieldResult.Add(historyItem);
                }

                if (yieldResult.Count > 0)
                {
                    await chunkCallback(yieldResult, priceType);
                }
            });
        }
    }
}
