// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    internal sealed class AssetPairCandlesHistoryRepository
    {
        private readonly string _assetPairId;
        private readonly INoSQLTableStorage<CandleHistoryEntity> _tableStorage;

        public AssetPairCandlesHistoryRepository(
            string assetPairId,
            INoSQLTableStorage<CandleHistoryEntity> tableStorage)
        {
            _assetPairId = assetPairId;
            _tableStorage = tableStorage;
        }
        
        public async Task<IEnumerable<ICandle>> GetCandlesAsync(CandlePriceType priceType, CandleTimeInterval interval, DateTime from, DateTime to)
        {
            if (priceType == CandlePriceType.Unspecified)
            {
                throw new ArgumentException(nameof(priceType));
            }

            var query = GetTableQuery(priceType, interval, from, to);
            var entities = await _tableStorage.WhereAsync(query);
            var candles = entities
                .SelectMany(e => e.Candles.Select(ci => ci.ToCandle(_assetPairId, e.PriceType, e.DateTime, interval)));

            return candles.Where(c => c.Timestamp >= from && c.Timestamp < to);
        }

        public async Task<ICandle> TryGetFirstCandleAsync(CandlePriceType priceType, CandleTimeInterval timeInterval)
        {
            var candleEntity = await _tableStorage.GetTopRecordAsync(CandleHistoryEntity.GeneratePartitionKey(priceType));

            return candleEntity
                ?.Candles
                .First()
                .ToCandle(_assetPairId, priceType, candleEntity.DateTime, timeInterval);
        }

        private static TableQuery<CandleHistoryEntity> GetTableQuery(
            CandlePriceType priceType,
            CandleTimeInterval interval,
            DateTime from,
            DateTime to)
        {
            var partitionKey = CandleHistoryEntity.GeneratePartitionKey(priceType);
            var rowKeyFrom = CandleHistoryEntity.GenerateRowKey(from, interval);
            var rowKeyTo = CandleHistoryEntity.GenerateRowKey(to, interval);

            var pkeyFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);

            var rowkeyFromFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, rowKeyFrom);
            var rowkeyToFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, rowKeyTo);
            var rowkeyFilter = TableQuery.CombineFilters(rowkeyFromFilter, TableOperators.And, rowkeyToFilter);

            return new TableQuery<CandleHistoryEntity>
            {
                FilterString = TableQuery.CombineFilters(pkeyFilter, TableOperators.And, rowkeyFilter)
            };
        }
    }
}
