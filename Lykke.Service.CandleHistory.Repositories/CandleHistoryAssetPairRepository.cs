using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.CandleHistory.Repositories
{
    internal sealed class CandleHistoryAssetPairRepository
    {
        private readonly INoSQLTableStorage<CandleTableEntity> _tableStorage;
        private readonly IAssetPairRepositoryHealthService _healthService;

        public CandleHistoryAssetPairRepository(INoSQLTableStorage<CandleTableEntity> tableStorage, IAssetPairRepositoryHealthService healthService)
        {
            _tableStorage = tableStorage;
            _healthService = healthService;
        }

        public async Task InsertOrMergeAsync(IEnumerable<IFeedCandle> candles, PriceType priceType, TimeInterval interval)
        {
            // Group by row
            var groups = candles.GroupBy(candle => new
            {
                PartitionKey = candle.PartitionKey(priceType),
                RowKey = candle.RowKey(interval)
            });

            int groupsCount = 0;

            // Update rows
            foreach (var group in groups)
            {
                await InsertOrMergeAsync(group.ToArray(), group.Key.PartitionKey, group.Key.RowKey, interval);
                ++groupsCount;
            }

            _healthService.TraceRowMergedGroupsCount(groupsCount);
        }

        private async Task InsertOrMergeAsync(IFeedCandle[] candles, string partitionKey, string rowKey, TimeInterval interval)
        {
            _healthService.TraceRowMergedCandlesCount(candles.Length);

            // Read row
            var entity = await _tableStorage.GetDataAsync(partitionKey, rowKey) ??
                         new CandleTableEntity(partitionKey, rowKey);

            // Merge all candles
            entity.MergeCandles(candles, interval);

            // Update
            await _tableStorage.InsertOrMergeAsync(entity);
        }

        public async Task<IEnumerable<IFeedCandle>> GetCandlesAsync(PriceType priceType, TimeInterval interval, DateTime from, DateTime to)
        {
            if (priceType == PriceType.Unspecified) { throw new ArgumentException(nameof(priceType)); }

            var partitionKey = CandleTableEntity.GeneratePartitionKey(priceType);
            var rowKeyFrom = CandleTableEntity.GenerateRowKey(from, interval);
            var rowKeyTo = CandleTableEntity.GenerateRowKey(to, interval);

            var query = new TableQuery<CandleTableEntity>();
            var pkeyFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);

            var rowkeyCondFrom = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, rowKeyFrom);
            var rowkeyCondTo = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, rowKeyTo);
            var rowkeyFilter = TableQuery.CombineFilters(rowkeyCondFrom, TableOperators.And, rowkeyCondTo);

            query.FilterString = TableQuery.CombineFilters(pkeyFilter, TableOperators.And, rowkeyFilter);

            var entities = await _tableStorage.WhereAsync(query);

            var result = from e in entities
                         select e.Candles.Select(ci => ci.ToCandle(e.PriceType == PriceType.Bid, e.DateTime, interval));

            return result
                .SelectMany(c => c)
                .Where(c => c.DateTime >= from && c.DateTime < to);
        }
    }
}