using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    /// <summary>
    /// Deserializes cache from the snapshot
    /// </summary>
    public interface ICandlesCacheDeserializationService
    {
        Task<bool> DeserializeCacheAsync();
    }
}