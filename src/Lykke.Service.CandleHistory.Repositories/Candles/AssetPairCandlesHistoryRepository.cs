using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    internal sealed class AssetPairCandlesHistoryRepository
    {
        private readonly string _assetPairId;
        private readonly CandleTimeInterval _timeInterval;
        private readonly INoSQLTableStorage<CandleHistoryEntity> _tableStorage;

        public AssetPairCandlesHistoryRepository(
            string assetPairId,
            CandleTimeInterval timeInterval,
            INoSQLTableStorage<CandleHistoryEntity> tableStorage)
        {
            _assetPairId = assetPairId;
            _timeInterval = timeInterval;
            _tableStorage = tableStorage;
        }

        /// <summary>
        /// Assumed that all candles have the same AssetPair, PriceType, and Timeinterval
        /// </summary>
        public async Task InsertOrMergeAsync(IReadOnlyCollection<ICandle> candles, CandlePriceType priceType)
        {
            foreach (var candle in candles)
            {
                if (candle.AssetPairId != _assetPairId)
                {
                    throw new ArgumentException($"Candle {candle.ToJson()} has invalid AssetPriceId", nameof(candles));
                }
                if (candle.TimeInterval != _timeInterval)
                {
                    throw new ArgumentException($"Candle {candle.ToJson()} has invalid TimeInterval", nameof(candles));
                }
                if (candle.PriceType != priceType)
                {
                    throw new ArgumentException($"Candle {candle.ToJson()} has invalid PriceType", nameof(candles));
                }
            }

            var partitionKey = CandleHistoryEntity.GeneratePartitionKey(priceType);

            var candleByRows = candles
                .GroupBy(candle => CandleHistoryEntity.GenerateRowKey(candle.Timestamp, _timeInterval))
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            // updates existing entities

            var existingEntities = (await _tableStorage.GetDataAsync(partitionKey, candleByRows.Keys)).ToArray();

            foreach (var entity in existingEntities)
            {
                entity.MergeCandles(candleByRows[entity.RowKey], _timeInterval);
            }

            // creates new entities

            var newEntityKeys = candleByRows.Keys.Except(existingEntities.Select(e => e.RowKey));
            var newEntities = newEntityKeys.Select(k => new CandleHistoryEntity(partitionKey, k)).ToArray();

            foreach (var entity in newEntities)
            {
                entity.MergeCandles(candleByRows[entity.RowKey], _timeInterval);
            }

            // save changes

            await _tableStorage.InsertOrReplaceBatchAsync(existingEntities.Concat(newEntities));
        }

        public async Task<IEnumerable<ICandle>> GetCandlesAsync(CandlePriceType priceType, CandleTimeInterval interval, DateTime from, DateTime? to)
        {
            if (priceType == CandlePriceType.Unspecified)
            {
                throw new ArgumentException(nameof(priceType));
            }

            var query = to.HasValue
                ? GetTableQuery(priceType, interval, from, to.Value)
                : GetTableQuery(priceType, interval, from);
            var entities = await _tableStorage.WhereAsync(query);
            var candles = entities
                .SelectMany(e => e.Candles.Select(ci => ci.ToCandle(_assetPairId, e.CandlePriceType, e.DateTime, interval)));

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
            DateTime from)
        {
            var partitionKey = CandleHistoryEntity.GeneratePartitionKey(priceType);
            var rowKey = CandleHistoryEntity.GenerateRowKey(from, interval);

            var pkeyFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            var rowkeyFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey);

            return new TableQuery<CandleHistoryEntity>
            {
                FilterString = TableQuery.CombineFilters(pkeyFilter, TableOperators.And, rowkeyFilter)
            };
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
