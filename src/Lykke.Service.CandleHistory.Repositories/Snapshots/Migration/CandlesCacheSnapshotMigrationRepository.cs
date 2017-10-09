using System.Collections.Immutable;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.CandleHistory.Repositories.Snapshots.Legacy;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandleHistory.Repositories.Snapshots.Migration
{
    public class CandlesCacheSnapshotMigrationRepository : ISnapshotRepository<IImmutableDictionary<string, IImmutableList<ICandle>>>
    {
        private readonly LegacyCandlesCacheSnapshotRepository _legacyRepository;
        private readonly CandlesCacheSnapshotRepository _repository;

        public CandlesCacheSnapshotMigrationRepository(IBlobStorage storage)
        {
            _legacyRepository = new LegacyCandlesCacheSnapshotRepository(storage);
            _repository = new CandlesCacheSnapshotRepository(storage);
        }

        public Task SaveAsync(IImmutableDictionary<string, IImmutableList<ICandle>> state)
        {
            return _repository.SaveAsync(state);
        }

        public async Task<IImmutableDictionary<string, IImmutableList<ICandle>>> TryGetAsync()
        {
            return await _legacyRepository.TryGetAsync() ?? await _repository.TryGetAsync();
        }
    }
}
