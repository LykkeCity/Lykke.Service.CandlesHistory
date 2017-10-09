using System.Collections.Immutable;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.CandleHistory.Repositories.Snapshots.Legacy;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandleHistory.Repositories.Snapshots.Migration
{
    public class CandlesPersistenceQueueSnapshotMigrationRepository : ISnapshotRepository<IImmutableList<ICandle>>
    {
        private readonly LegacyCandlesPersistenceQueueSnapshotRepository _legacyRepository;
        private readonly CandlesPersistenceQueueSnapshotRepository _repository;

        public CandlesPersistenceQueueSnapshotMigrationRepository(IBlobStorage storage)
        {
            _legacyRepository = new LegacyCandlesPersistenceQueueSnapshotRepository(storage);
            _repository = new CandlesPersistenceQueueSnapshotRepository(storage);
        }

        public Task SaveAsync(IImmutableList<ICandle> state)
        {
            return _repository.SaveAsync(state);
        }

        public async Task<IImmutableList<ICandle>> TryGetAsync()
        {
            return await _legacyRepository.TryGetAsync() ?? await _repository.TryGetAsync();
        }
    }
}
