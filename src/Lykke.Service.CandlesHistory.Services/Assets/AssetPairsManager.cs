﻿using System;
using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Polly;
using Lykke.Common.Log;
using Common.Log;

namespace Lykke.Service.CandlesHistory.Services.Assets
{
    public class AssetPairsManager : IAssetPairsManager
    {
        private readonly ILog _log;
        private readonly IAssetsServiceWithCache _apiService;

        public AssetPairsManager(ILogFactory logFactory, IAssetsServiceWithCache apiService)
        {
            _log = logFactory.CreateLog(this);
            _apiService = apiService;
        }

        public Task<AssetPair> TryGetAssetPairAsync(string assetPairId)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timespan) => _log.Error(exception, context: new { assetPairId = assetPairId }))
                .ExecuteAsync(() => _apiService.TryGetAssetPairAsync(assetPairId));
        }

        public async Task<AssetPair> TryGetEnabledPairAsync(string assetPairId)
        {
            var pair = await TryGetAssetPairAsync(assetPairId);

            return pair == null || pair.IsDisabled ? null : pair;
        }

        public Task<IEnumerable<AssetPair>> GetAllEnabledAsync()
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timespan) => _log.Error(exception))
                .ExecuteAsync(async () => (await _apiService.GetAllAssetPairsAsync()).Where(a => !a.IsDisabled));
        }

        public Task<IEnumerable<AssetPair>> GetAllAsync()
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timespan) => _log.Error(exception))
                .ExecuteAsync(async () => (await _apiService.GetAllAssetPairsAsync()).AsEnumerable());
        }
    }
}
