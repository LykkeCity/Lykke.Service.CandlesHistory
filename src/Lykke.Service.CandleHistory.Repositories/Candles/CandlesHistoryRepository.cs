using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.SettingsReader;

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    public class CandlesHistoryRepository : ICandlesHistoryRepository
    {
        private readonly ILog _log;
        private readonly IReloadingManager<Dictionary<string, string>> _assetConnectionStrings;

        private readonly ConcurrentDictionary<string, AssetPairCandlesHistoryRepository> _assetPairRepositories;

        public CandlesHistoryRepository(ILog log, IReloadingManager<Dictionary<string, string>> assetConnectionStrings)
        {
            _log = log;
            _assetConnectionStrings = assetConnectionStrings;

            _assetPairRepositories = new ConcurrentDictionary<string, AssetPairCandlesHistoryRepository>();
        }

        public bool CanStoreAssetPair(string assetPairId)
        {
            return _assetConnectionStrings.CurrentValue.ContainsKey(assetPairId);
        }

        /// <summary>
        /// Insert or merge candles. Assumed that all candles have the same AssetPairId, PriceType, Timeinterval
        /// </summary>
        public async Task InsertOrMergeAsync(IReadOnlyCollection<ICandle> candles, string assetPairId, PriceType priceType, TimeInterval timeInterval)
        {
            if (!candles.Any())
            {
                return;
            }

            foreach (var candle in candles)
            {
                if (candle.AssetPairId != assetPairId)
                {
                    throw new ArgumentException($"Candle {candle.ToJson()} has invalid AssetPriceId", nameof(candles));
                }
                if (candle.TimeInterval != timeInterval)
                {
                    throw new ArgumentException($"Candle {candle.ToJson()} has invalid TimeInterval", nameof(candles));
                }
            }

            var repo = GetRepo(assetPairId, timeInterval);
            try
            {
                await repo.InsertOrMergeAsync(candles, priceType);
            }
            catch
            {
                ResetRepo(assetPairId, timeInterval);
                throw;
            }
        }

        /// <summary>
        /// Returns buy or sell candle values for the specified interval from the specified time range.
        /// </summary>
        public async Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, TimeInterval interval, PriceType priceType, DateTime from, DateTime to)
        {
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

        public async Task<DateTime?> GetFirstCandleDateTimeAsync(string assetPairId, TimeInterval interval, PriceType priceType)
        {
            var repo = GetRepo(assetPairId, interval);
            try
            {
                return await repo.GetFirstCandleDateTimeAsync(priceType, interval);
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

        private AssetPairCandlesHistoryRepository GetRepo(string assetPairId, TimeInterval timeInterval)
        {
            var tableName = timeInterval.ToString().ToLowerInvariant();
            var key = $"{assetPairId.ToLowerInvariant()}_{tableName}";

            if (!_assetPairRepositories.TryGetValue(key, out AssetPairCandlesHistoryRepository repo) || repo == null)
            {
                return _assetPairRepositories.AddOrUpdate(
                    key: key,
                    addValueFactory: k => new AssetPairCandlesHistoryRepository(assetPairId, timeInterval, CreateStorage(assetPairId, tableName)),
                    updateValueFactory: (k, oldRepo) => oldRepo ?? new AssetPairCandlesHistoryRepository(assetPairId, timeInterval, CreateStorage(assetPairId, tableName)));
            }

            return repo;
        }

        private INoSQLTableStorage<CandleHistoryEntity> CreateStorage(string assetPairId, string tableName)
        {
            if (!_assetConnectionStrings.CurrentValue.TryGetValue(assetPairId, out var assetConnectionString) ||
                string.IsNullOrEmpty(assetConnectionString))
            {
                throw new ConfigurationException($"Connection string for asset pair '{assetPairId}' is not specified.");
            }

            var storage = AzureTableStorage<CandleHistoryEntity>.Create(
                _assetConnectionStrings.ConnectionString(x => x[assetPairId]), 
                tableName, 
                _log);

            // Create and preload table info
            storage.GetDataAsync(assetPairId, "1900-01-01").Wait();

            return storage;
        }
    }
}
