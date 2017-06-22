using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Domain
{
    public interface IAssetPairsRepository
    {
        Task<IEnumerable<IAssetPair>> GetAllAsync();
    }
}