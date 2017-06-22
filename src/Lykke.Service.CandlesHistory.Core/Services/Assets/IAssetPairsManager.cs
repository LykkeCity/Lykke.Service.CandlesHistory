using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Core.Domain;
using System.Collections.Generic;

namespace Lykke.Service.CandlesHistory.Core.Services.Assets
{
    public interface IAssetPairsManager
    {
        Task<IAssetPair> TryGetEnabledPairAsync(string assetPairId);
        Task<IEnumerable<IAssetPair>> GetAllEnabledAsync();
    }
}