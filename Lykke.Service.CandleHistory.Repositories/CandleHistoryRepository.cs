using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandleHistory.Repositories
{
    public class CandleHistoryRepository : ICandleHistoryRepository
    {
        private readonly CreateStorage _createStorage;
        private readonly ConcurrentDictionary<string, CandleHistoryAssetPairRepository> _assetPairRepositories;

        public CandleHistoryRepository(CreateStorage createStorage)
        {
            _createStorage = createStorage;
            _assetPairRepositories = new ConcurrentDictionary<string, CandleHistoryAssetPairRepository>();
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
                return _assetPairRepositories.AddOrUpdate(
                    key: key,
                    addValueFactory: k => new CandleHistoryAssetPairRepository(_createStorage(assetPairId, tableName)),
                    updateValueFactory: (k, oldRepo) => oldRepo ?? new CandleHistoryAssetPairRepository(_createStorage(assetPairId, tableName)));
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
    }
}