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
using Lykke.Service.CandlesHistory.Core.Services.Assets;

namespace Lykke.Service.CandlesHistory.Services.Assets
{
    public class MtAssetPairsManager : TimerPeriod, IAssetPairsManager
    {
        private readonly IAssetPairsApi _assetPairsApi;
        private readonly ILog _log;
        
        private Dictionary<string, AssetPair> _cache = new Dictionary<string, AssetPair>();
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        public MtAssetPairsManager(IAssetPairsApi assetPairsApi, TimeSpan expirationPeriod, ILog log)
            : base(nameof(MtAssetPairsManager), (int)expirationPeriod.TotalMilliseconds, log)
        {
            _assetPairsApi = assetPairsApi;
            _log = log;
        }

        public Task<AssetPair> TryGetEnabledPairAsync(string assetPairId)
        {
            if (string.IsNullOrWhiteSpace(assetPairId))
            {
                return Task.FromResult((AssetPair)null);
            }
            
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                return _cache.TryGetValue(assetPairId, out var assetPair)
                    ? Task.FromResult(assetPair)
                    : Task.FromResult((AssetPair)null);
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
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
            
            Execute().GetAwaiter().GetResult();
        }

        public override async Task Execute()
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
