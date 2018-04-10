using System.Threading.Tasks;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Core.Domain;

namespace Lykke.Service.CandlesHistory.Core.Services.Assets
{
    public interface IAssetPairsManager
    {
        [ItemCanBeNull] Task<AssetPair> TryGetAssetPairAsync(string assetPairId);
        [ItemCanBeNull] Task<AssetPair> TryGetEnabledPairAsync(string assetPairId);
        Task<IEnumerable<AssetPair>> GetAllEnabledAsync();
    }
}
