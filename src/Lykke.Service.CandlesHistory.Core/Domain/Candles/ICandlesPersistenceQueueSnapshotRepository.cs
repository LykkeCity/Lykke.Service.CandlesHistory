using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public interface ICandlesPersistenceQueueSnapshotRepository
    {
        Task SaveAsync(AssetPairCandle[] state);
        Task<AssetPairCandle[]> TryGetAsync();
    }
}