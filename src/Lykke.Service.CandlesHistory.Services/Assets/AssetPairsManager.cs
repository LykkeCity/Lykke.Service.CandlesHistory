using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using System.Collections.Generic;
using Lykke.Service.CandlesHistory.Core.Services;

namespace Lykke.Service.CandlesHistory.Services.Assets
{
    public class AssetPairsManager : IAssetPairsManager
    {
        private readonly IAssetPairsRepository _repository;
        private readonly IAssetPairsCacheService _cache;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly TimeSpan _cacheExpirationPeriod;
        private DateTime _cacheExpirationMoment;

        public AssetPairsManager(IAssetPairsRepository repository, IAssetPairsCacheService cache, IDateTimeProvider dateTimeProvider, TimeSpan cacheExpirationPeriod)
        {
            _repository = repository;
            _cache = cache;
            _dateTimeProvider = dateTimeProvider;
            _cacheExpirationPeriod = cacheExpirationPeriod;

            _cacheExpirationMoment = DateTime.MinValue;
        }

        public async Task<IAssetPair> TryGetEnabledPairAsync(string assetPairId)
        {
            await EnsureCacheIsUpdatedAsync();

            var pair = _cache.TryGetPair(assetPairId);

            return pair == null || pair.IsDisabled ? null : pair;
        }

        public async Task<IEnumerable<IAssetPair>> GetAllEnabledAsync()
        {
            await EnsureCacheIsUpdatedAsync();

            return _cache.GetAll().Where(a => !a.IsDisabled);
        }

        private async Task EnsureCacheIsUpdatedAsync()
        {
            if (_cacheExpirationMoment < _dateTimeProvider.UtcNow)
            {
                await UpdateCacheAsync();
                _cacheExpirationMoment = _dateTimeProvider.UtcNow + _cacheExpirationPeriod;
            }
        }

        private async Task UpdateCacheAsync()
        {
            var pairs = await _repository.GetAllAsync();

            _cache.Update(pairs);
        }
    }
}