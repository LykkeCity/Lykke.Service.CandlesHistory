using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;

namespace Lykke.Service.CandleHistory.Repositories.HistoryMigration
{
    public class FeedHistoryRepository : IFeedHistoryRepository
    {
        private readonly INoSQLTableStorage<FeedHistoryEntity> _tableStorage;

        public FeedHistoryRepository(INoSQLTableStorage<FeedHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IFeedHistory> GetTopRecordAsync(string assetPair)
        {
            return await _tableStorage.GetTopRecordAsync($"{assetPair}_Ask") ?? 
                await _tableStorage.GetTopRecordAsync($"{assetPair}_Bid");
        }

        public async Task<IFeedHistory> GetCandle(string assetPair, PriceType priceType, string date)
        {
            return await _tableStorage.GetDataAsync(FeedHistoryEntity.GeneratePartitionKey(assetPair, priceType), date);
        }

        public Task GetCandlesByChunkAsync(string assetPair, PriceType priceType, DateTime startDate, DateTime endDate, Func<IEnumerable<IFeedHistory>, PriceType, Task> chunkCallback)
        {
            return _tableStorage.GetDataByChunksAsync(FeedHistoryEntity.GeneratePartitionKey(assetPair, priceType), async chunk =>
            {
                var yieldResult = new List<IFeedHistory>();

                foreach (var historyItem in chunk.Where(item => item.DateTime >= startDate && item.DateTime <= endDate))
                {
                    yieldResult.Add(historyItem);
                }

                if (yieldResult.Count > 0)
                {
                    await chunkCallback(yieldResult, priceType);
                    yieldResult.Clear();
                }
            });
        }
    }
}
