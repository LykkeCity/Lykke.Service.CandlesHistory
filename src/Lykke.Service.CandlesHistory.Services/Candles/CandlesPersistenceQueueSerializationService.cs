using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesPersistenceQueueSerializationService : ICandlesPersistenceQueueSerializationService
    {
        private readonly ICandlesPersistenceQueueSnapshotRepository _repository;
        private readonly ICandlesPersistenceQueue _queue;
        private readonly ILog _log;

        public CandlesPersistenceQueueSerializationService(
            ICandlesPersistenceQueueSnapshotRepository repository,
            ICandlesPersistenceQueue queue,
            ILog log)
        {
            _repository = repository;
            _queue = queue;
            _log = log;
        }

        public async Task SerializeQueueAsync()
        {
            var state = _queue.GetState();

            await _log.WriteInfoAsync(nameof(CandlesPersistenceQueueSerializationService), nameof(SerializeQueueAsync), "",
                $"Serializing {state.Length} candles");

            await _repository.SaveAsync(state);
        }
    }
}