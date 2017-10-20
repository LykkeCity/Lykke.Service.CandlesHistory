using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    internal sealed class AssetPairCandlesHistoryRepository
    {
        private readonly string _assetPairId;
        private readonly TimeInterval _timeInterval;
        private readonly INoSQLTableStorage<CandleHistoryEntity> _tableStorage;

        public AssetPairCandlesHistoryRepository(
            string assetPairId,
            TimeInterval timeInterval,
            INoSQLTableStorage<CandleHistoryEntity> tableStorage)
        {
            _assetPairId = assetPairId;
            _timeInterval = timeInterval;
            _tableStorage = tableStorage;
        }

        /// <summary>
        /// Assumed that all candles have the same PriceType, Timeinterval and Timestamp
        /// </summary>
        public async Task InsertOrMergeAsync(IReadOnlyCollection<ICandle> candles, PriceType priceType)
        {
            foreach (var candle in candles)
            {
                if (candle.PriceType != priceType)
                {
                    throw new ArgumentException($"Candle {candle.ToJson()} has invalid PriceType", nameof(candles));
                }
            }

            var partitionKey = CandleHistoryEntity.GeneratePartitionKey(priceType);
            
            // Group by row
            var groups = candles.GroupBy(candle => CandleHistoryEntity.GenerateRowKey(candle.Timestamp, _timeInterval));

            foreach (var group in groups)
            {
                await InsertOrMergeAsync(group, partitionKey, group.Key, _timeInterval);
            }
        }

        private async Task InsertOrMergeAsync(IEnumerable<ICandle> candle, string partitionKey, string rowKey, TimeInterval timeInterval)
        {
            // Read row
            var entity = await _tableStorage.GetDataAsync(partitionKey, rowKey) ??
                         new CandleHistoryEntity(partitionKey, rowKey);

            // Merge all candles
            entity.MergeCandles(candle, timeInterval);

            // Update
            await _tableStorage.InsertOrMergeAsync(entity);
        }

        public async Task<IEnumerable<ICandle>> GetCandlesAsync(PriceType priceType, TimeInterval interval, DateTime from, DateTime to)
        {
            if (priceType == PriceType.Unspecified) { throw new ArgumentException(nameof(priceType)); }

            var partitionKey = CandleHistoryEntity.GeneratePartitionKey(priceType);
            var rowKeyFrom = CandleHistoryEntity.GenerateRowKey(from, interval);
            var rowKeyTo = CandleHistoryEntity.GenerateRowKey(to, interval);

            var query = new TableQuery<CandleHistoryEntity>();
            var pkeyFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);

            var rowkeyCondFrom = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, rowKeyFrom);
            var rowkeyCondTo = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, rowKeyTo);
            var rowkeyFilter = TableQuery.CombineFilters(rowkeyCondFrom, TableOperators.And, rowkeyCondTo);

            query.FilterString = TableQuery.CombineFilters(pkeyFilter, TableOperators.And, rowkeyFilter);

            var entities = await _tableStorage.WhereAsync(query);

            var result = from e in entities
                         select e.Candles.Select(ci => ci.ToCandle(_assetPairId, e.PriceType, e.DateTime, interval));

            return result
                .SelectMany(c => c)
                .Where(c => c.Timestamp >= from && c.Timestamp < to);
        }

        public async Task<DateTime?> GetTopRecordDateTimeAsync(PriceType priceType)
        {
            return (await _tableStorage.GetTopRecordAsync(CandleHistoryEntity.GeneratePartitionKey(priceType)))?.DateTime;
        }
    }
}
