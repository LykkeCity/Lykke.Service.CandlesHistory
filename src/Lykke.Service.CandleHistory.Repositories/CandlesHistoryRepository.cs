using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using AzureStorage.Tables.Decorators;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;

namespace Lykke.Service.CandleHistory.Repositories
{
    public class CandlesHistoryRepository : ICandlesHistoryRepository
    {
        private readonly IHealthService _healthService;
        private readonly ILog _log;
        private readonly IImmutableDictionary<string, string> _assetConnectionStrings;

        private readonly ConcurrentDictionary<string, CandleHistoryAssetPairRepository> _assetPairRepositories;

        public CandlesHistoryRepository(
            IHealthService healthService,
            ILog log,
            IImmutableDictionary<string, string> assetConnectionStrings)
        {
            _healthService = healthService;
            _log = log;
            _assetConnectionStrings = assetConnectionStrings;

            _assetPairRepositories = new ConcurrentDictionary<string, CandleHistoryAssetPairRepository>();
        }

        public bool CanStoreAssetPair(string assetPairId)
        {
            return _assetConnectionStrings.ContainsKey(assetPairId);
        }

        /// <summary>
        /// Insert or merge candle value.
        /// </summary>
        public async Task InsertOrMergeAsync(IEnumerable<IFeedCandle> candles, string assetPairId, TimeInterval interval, PriceType priceType)
        {
            ValidateAndThrow(assetPairId, interval, priceType);
            var repo = GetRepo(assetPairId, interval);
            try
            {
                await repo.InsertOrMergeAsync(candles, priceType, interval);
            }
            catch
            {
                ResetRepo(assetPairId, interval);
                throw;
            }
        }

        /// <summary>
        /// Returns buy or sell candle values for the specified interval from the specified time range.
        /// </summary>
        public async Task<IEnumerable<IFeedCandle>> GetCandlesAsync(string assetPairId, TimeInterval interval, PriceType priceType, DateTime from, DateTime to)
        {
            ValidateAndThrow(assetPairId, interval, priceType);
            var repo = GetRepo(assetPairId, interval);
            try
            {
                return await repo.GetCandlesAsync(priceType, interval, from, to);
            }
            catch
            {
                ResetRepo(assetPairId, interval);
                throw;
            }
        }

        private void ResetRepo(string assetPairId, TimeInterval interval)
        {
            var tableName = interval.ToString().ToLowerInvariant();
            var key = assetPairId.ToLowerInvariant() + "_" + tableName;

            _assetPairRepositories[key] = null;
        }

        private CandleHistoryAssetPairRepository GetRepo(string assetPairId, TimeInterval interval)
        {
            var tableName = interval.ToString().ToLowerInvariant();
            var key = assetPairId.ToLowerInvariant() + "_" + tableName;

            if (!_assetPairRepositories.TryGetValue(key, out CandleHistoryAssetPairRepository repo) || repo == null)
            {
                var repositoryHealthService = _healthService.GetAssetPairRepositoryHealth(key);
                return _assetPairRepositories.AddOrUpdate(
                    key: key,
                    addValueFactory: k => new CandleHistoryAssetPairRepository(CreateStorage(assetPairId, tableName), repositoryHealthService),
                    updateValueFactory: (k, oldRepo) => oldRepo ?? new CandleHistoryAssetPairRepository(CreateStorage(assetPairId, tableName), repositoryHealthService));
            }

            return repo;
        }

        private void ValidateAndThrow(string assetPairId, TimeInterval interval, PriceType priceType)
        {
            if (string.IsNullOrEmpty(assetPairId))
            {
                throw new ArgumentNullException(nameof(assetPairId));
            }
            if (interval == TimeInterval.Unspecified)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "Time interval is not specified");
            }
            if (priceType == PriceType.Unspecified)
            {
                throw new ArgumentOutOfRangeException(nameof(priceType), "Price type is not specified");
            }
        }

        private INoSQLTableStorage<CandleTableEntity> CreateStorage(string assetPairId, string tableName)
        {
            if (!_assetConnectionStrings.TryGetValue(assetPairId, out string assetConnectionString) ||
                string.IsNullOrEmpty(assetConnectionString))
            {
                throw new ConfigurationException($"Connection string for asset pair '{assetPairId}' is not specified.");
            }

            var storage = AzureTableStorage<CandleTableEntity>.Create(() => assetConnectionString, tableName, _log);

            // Create and preload table info
            storage.GetDataAsync(assetPairId, "1900-01-01").Wait();

            return new RetryOnFailureAzureTableStorageDecorator<CandleTableEntity>(storage, 5, 5, TimeSpan.FromSeconds(10));
        }
    }
}