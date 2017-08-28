using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesCacheDeserializationService : ICandlesCacheDeserializationService
    {
        private readonly ICandlesCacheSnapshotRepository _repository;
        private readonly ICandlesCacheService _cache;
        private readonly ILog _log;

        public CandlesCacheDeserializationService(ICandlesCacheSnapshotRepository repository, ICandlesCacheService cache, ILog log)
        {
            _repository = repository;
            _cache = cache;
            _log = log;
        }

        public async Task<bool> DeserializeCacheAsync()
        {
            var state = await _repository.TryGetAsync();

            if (state == null)
            {
                await _log.WriteWarningAsync(nameof(CandlesCacheDeserializationService), nameof(DeserializeCacheAsync), "",
                    "No cache snapshot found to deserialize");

                return false;
            }

            _cache.SetState(state);

            return true;
        }
    }
}