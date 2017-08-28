using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    /// <summary>
    /// Deserializes persistence  queue from the snapshot
    /// </summary>
    public interface ICandlesPersistenceQueueDeserializationService
    {
        Task<bool> DeserializeQueueAsync();
    }
}