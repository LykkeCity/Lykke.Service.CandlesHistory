using System.Collections.Generic;
using Lykke.Service.CandlesHistory.Core.Domain;

namespace Lykke.Service.CandlesHistory.Core.Services.Assets
{
    public interface IAssetPairsCacheService
    {
        void Update(IEnumerable<IAssetPair> pairs);
        IAssetPair TryGetPair(string assetPairId);
        IReadOnlyCollection<IAssetPair> GetAll();
    }
}