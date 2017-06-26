using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Assets.Client.Custom;

namespace Lykke.Service.CandlesHistory.Services.Assets
{
    public class AssetPairsManager : IAssetPairsManager
    {
        private readonly ICachedAssetsService _apiService;

        public AssetPairsManager(ICachedAssetsService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IAssetPair> TryGetEnabledPairAsync(string assetPairId)
        {
            var pair = await _apiService.TryGetAssetPairAsync(assetPairId);

            return pair == null || pair.IsDisabled ? null : pair;
        }

        public async Task<IEnumerable<IAssetPair>> GetAllEnabledAsync()
        {
            return (await _apiService.GetAllAssetPairsAsync()).Where(a => !a.IsDisabled);
        }
    }
}