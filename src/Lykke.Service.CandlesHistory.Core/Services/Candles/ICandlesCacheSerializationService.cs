using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    /// <summary>
    /// Serializes cache to the snapshot
    /// </summary>
    public interface ICandlesCacheSerializationService
    {
        Task SerializeCacheAsync();
    }
}