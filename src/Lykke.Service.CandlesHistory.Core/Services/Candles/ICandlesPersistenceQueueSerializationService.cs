using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    /// <summary>
    /// Serializes persistence queue to the snapshot
    /// </summary>
    public interface ICandlesPersistenceQueueSerializationService
    {
        Task SerializeQueueAsync();
    }
}