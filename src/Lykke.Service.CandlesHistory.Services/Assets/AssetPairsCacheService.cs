using System.Collections.Generic;
using System.Linq;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Services.Assets;

namespace Lykke.Service.CandlesHistory.Services.Assets
{
    public class AssetPairsCacheService : IAssetPairsCacheService
    {
        private Dictionary<string, IAssetPair> _pairs = new Dictionary<string, IAssetPair>();

        public void Update(IEnumerable<IAssetPair> pairs)
        {
            _pairs = pairs.ToDictionary(p => p.Id, p => p);
        }

        public IAssetPair TryGetPair(string assetPairId)
        {
            _pairs.TryGetValue(assetPairId, out IAssetPair pair);

            return pair;
        }

        public IReadOnlyCollection<IAssetPair> GetAll()
        {
            return _pairs.Values;
        }
    }
}