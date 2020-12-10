// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Common.Log;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using MarginTrading.SettingsService.Contracts;
using Polly;
using System;
using System.Threading;
using Common;
using Lykke.Common.Log;
using Lykke.Service.CandlesHistory.Core.Services.Assets;

namespace Lykke.Service.CandlesHistory.Services.Assets
{
    public class MtAssetPairsManager : TimerPeriod, IAssetPairsManager
    {
        private readonly IAssetPairsApi _assetPairsApi;
        private readonly ILog _log;
        private DateTime _cacheInvalidatedAt;
        private readonly TimeSpan _cacheInvalidationProtectionPeriod;

        private Dictionary<string, AssetPair> _cache = new Dictionary<string, AssetPair>();
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        public MtAssetPairsManager(IAssetPairsApi assetPairsApi, TimeSpan expirationPeriod, ILog log)
            : base(nameof(MtAssetPairsManager), (int)expirationPeriod.TotalMilliseconds, log)
        {
            _assetPairsApi = assetPairsApi;
            _log = log;
            _cacheInvalidatedAt = DateTime.UtcNow;
            _cacheInvalidationProtectionPeriod = TimeSpan.FromSeconds(30);
        }

        public async Task<AssetPair> TryGetEnabledPairAsync(string assetPairId)
        {
            if (string.IsNullOrWhiteSpace(assetPairId))
            {
                return null;
            }

            AssetPair Get()
            {
                _readerWriterLockSlim.EnterReadLock();

                try
                {
                    return _cache.TryGetValue(assetPairId, out var assetPair)
                        ? assetPair
                        : null;
                }
                finally
                {
                    _readerWriterLockSlim.ExitReadLock();
                }
            }
            var result=  Get();

            //TODO use Interlocked.Exchange method to be thread safe
            //while comparing DateTime.UtcNow - _cacheInvalidatedAt > _cacheInvalidationProtectionPeriods
            //it doesnt really matter cause in worst case we would update cache twice
            //https://stackoverflow.com/questions/13042045/interlocked-compareexchangeint-using-greaterthan-or-lessthan-instead-of-equali
            if (result == null && DateTime.UtcNow - _cacheInvalidatedAt > _cacheInvalidationProtectionPeriod)
            {
                _log.Warning($"Forcibly invalidating cache, because asset pair {assetPairId} not found");

                _cacheInvalidatedAt = DateTime.UtcNow;
                await InvalidateCache();

                result = Get();
            }

            return result;
        }

        public Task<AssetPair> TryGetAssetPairAsync(string assetPairId)
        {
            return TryGetEnabledPairAsync(assetPairId);
        }


        public Task<IEnumerable<AssetPair>> GetAllEnabledAsync()
        {
            _readerWriterLockSlim.EnterReadLock();
            try
            {
                return Task.FromResult(_cache.Values.AsEnumerable());
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public override void Start()
        {
            base.Start();

            InvalidateCache().GetAwaiter().GetResult();
        }

        public override async Task Execute()
        {
            await InvalidateCache();
        }

        private async Task InvalidateCache()
        {
            var assetPairs = (await _assetPairsApi.List())?
                .ToDictionary(pair => pair.Id, pair => new AssetPair
                {
                    Id = pair.Id,
                    Name = pair.Name,
                    BaseAssetId = pair.BaseAssetId,
                    QuotingAssetId = pair.QuoteAssetId,
                    Accuracy = pair.Accuracy,
                    InvertedAccuracy = pair.Accuracy
                }) ?? new Dictionary<string, AssetPair>();

            _readerWriterLockSlim.EnterWriteLock();
            try
            {
                _cache = assetPairs;
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }
    }
}
