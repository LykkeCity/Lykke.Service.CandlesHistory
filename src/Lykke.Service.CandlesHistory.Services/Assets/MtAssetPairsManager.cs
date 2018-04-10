using System;
using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using System.Collections.Generic;
using System.Linq;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain;
using MarginTrading.Backend.Contracts.DataReaderClient;
using Polly;
using Polly.Retry;

namespace Lykke.Service.CandlesHistory.Services.Assets
{
    public class MtAssetPairsManager : IAssetPairsManager
    {
        private readonly IMtDataReaderClient _mtDataReaderClient;
        private readonly RetryPolicy _retryPolicy;

        public MtAssetPairsManager(ILog log, IMtDataReaderClient mtDataReaderClient)
        {
            _mtDataReaderClient = mtDataReaderClient;
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, retryAttempt))),
                    (exception, timespan) =>
                        log.WriteErrorAsync("Get all mt asset pairs with retry", string.Empty, exception));
        }

        public Task<AssetPair> TryGetAssetPairAsync(string assetPairId)
        {
            return TryGetEnabledPairAsync(assetPairId);
        }

        public async Task<AssetPair> TryGetEnabledPairAsync(string assetPairId)
        {
            return (await GetAllEnabledAsync()).FirstOrDefault(p => p.Id == assetPairId);
        }

        public Task<IEnumerable<AssetPair>> GetAllEnabledAsync()
        {
            // note the mtDataReaderClient caches the assetPairs for 3 minutes 
            return _retryPolicy.ExecuteAsync(async () =>
                (await _mtDataReaderClient.AssetPairsRead.List()).Select(p => new AssetPair(p.Id, p.Accuracy)));
        }
    }
}
