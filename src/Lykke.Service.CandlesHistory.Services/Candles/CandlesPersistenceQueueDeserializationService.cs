using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesPersistenceQueueDeserializationService : ICandlesPersistenceQueueDeserializationService
    {
        private readonly ICandlesPersistenceQueueSnapshotRepository _repository;
        private readonly ICandlesPersistenceQueue _queue;
        private readonly ILog _log;

        public CandlesPersistenceQueueDeserializationService(
            ICandlesPersistenceQueueSnapshotRepository repository, 
            ICandlesPersistenceQueue queue,
            ILog log)
        {
            _repository = repository;
            _queue = queue;
            _log = log;
        }

        public async Task<bool> DeserializeQueueAsync()
        {
            var state = await _repository.TryGetAsync();

            if (state == null)
            {
                await _log.WriteWarningAsync(nameof(CandlesPersistenceQueueDeserializationService), nameof(DeserializeQueueAsync), "",
                    "No persistence queue snapshot found to deserialize");

                return false;
            }

            await _log.WriteInfoAsync(nameof(CandlesPersistenceQueueDeserializationService), nameof(DeserializeQueueAsync), "",
                $"{state.Length} candles was deserialized");

            _queue.SetState(state);

            return true;
        }
    }
}