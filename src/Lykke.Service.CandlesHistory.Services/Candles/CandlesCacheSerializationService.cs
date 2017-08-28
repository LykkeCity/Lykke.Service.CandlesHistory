using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesCacheSerializationService : ICandlesCacheSerializationService
    {
        private readonly ICandlesCacheSnapshotRepository _repository;
        private readonly ICandlesCacheService _cache;

        public CandlesCacheSerializationService(ICandlesCacheSnapshotRepository repository, ICandlesCacheService cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task SerializeCacheAsync()
        {
            var state = _cache.GetState();

            await _repository.SaveAsync(state);
        }
    }
}