// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.SettingsReader;

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public sealed class CandlesHistoryRepository : ICandlesHistoryRepository, IDisposable
    {
        private readonly ILog _log;
        private readonly IReloadingManager<string> _assetConnectionString;

        private readonly Dictionary<string, AssetPairCandlesHistoryRepository> _assetPairRepositories;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public CandlesHistoryRepository(ILog log, IReloadingManager<string> assetConnectionString)
        {
            _log = log;
            _assetConnectionString = assetConnectionString;

            _assetPairRepositories = new Dictionary<string, AssetPairCandlesHistoryRepository>();
        }

        /// <summary>
        /// Returns buy or sell candle values for the specified interval from the specified time range.
        /// </summary>
        public async Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, CandleTimeInterval interval, CandlePriceType priceType, DateTime from, DateTime to)
        {
            var repo = await GetRepo(assetPairId, interval);
            try
            {
                var result = await repo.GetCandlesAsync(priceType, interval, from, to);
                return result;
            }
            catch
            {
                await ResetRepo(assetPairId, interval);
                throw;
            }
        }

        public async Task<ICandle> TryGetFirstCandleAsync(string assetPairId, CandleTimeInterval interval, CandlePriceType priceType)
        {
            var repo = await GetRepo(assetPairId, interval);
            try
            {
                return await repo.TryGetFirstCandleAsync(priceType, interval);
            }
            catch
            {
                await ResetRepo(assetPairId, interval);
                throw;
            }
        }

        private async Task ResetRepo(string assetPairId, CandleTimeInterval interval)
        {
            var tableName = interval.ToString().ToLowerInvariant();
            var key = $"{assetPairId}_{tableName}";
            try
            {
                await _semaphore.WaitAsync();
                _assetPairRepositories.Remove(key, out _);

            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<AssetPairCandlesHistoryRepository> GetRepo(string assetPairId, CandleTimeInterval timeInterval)
        {
            var tableName = timeInterval.ToString().ToLowerInvariant();
            var key = $"{assetPairId}_{tableName}";
            try
            {
                await _semaphore.WaitAsync();

                if (!_assetPairRepositories.TryGetValue(key, out var repo))
                {
                    repo = _assetPairRepositories[key] = new AssetPairCandlesHistoryRepository(assetPairId, CreateStorage(assetPairId, tableName));
                }
                return repo;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private INoSQLTableStorage<CandleHistoryEntity> CreateStorage(string assetPairId, string tableName)
        {
            var storage = AzureTableStorage<CandleHistoryEntity>.Create(
                _assetConnectionString,
                tableName,
                _log,
                TimeSpan.FromMinutes(1),
                onGettingRetryCount: 10,
                onModificationRetryCount: 10,
                retryDelay: TimeSpan.FromSeconds(1));

            // Create and preload table info
            storage.GetDataAsync(assetPairId, "1900-01-01").GetAwaiter().GetResult();

            return storage;
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}
