using System.Collections.Immutable;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using Lykke.Service.CandleHistory.Repositories.Snapshots.Legacy;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandleHistory.Repositories.Snapshots.Migration
{
    public class CandlesPersistenceQueueSnapshotMigrationRepository : ISnapshotRepository<IImmutableList<ICandle>>
    {
        private readonly ILog _log;
        private readonly LegacyCandlesPersistenceQueueSnapshotRepository _legacyRepository;
        private readonly CandlesPersistenceQueueSnapshotRepository _repository;

        public CandlesPersistenceQueueSnapshotMigrationRepository(IBlobStorage storage, ILog log)
        {
            _log = log;
            _legacyRepository = new LegacyCandlesPersistenceQueueSnapshotRepository(storage);
            _repository = new CandlesPersistenceQueueSnapshotRepository(storage);
        }

        public Task SaveAsync(IImmutableList<ICandle> state)
        {
            return _repository.SaveAsync(state);
        }

        public async Task<IImmutableList<ICandle>> TryGetAsync()
        {
            var newResult = await _repository.TryGetAsync();
            if (newResult == null)
            {
                await _log.WriteWarningAsync(nameof(CandlesPersistenceQueueSnapshotMigrationRepository), nameof(TryGetAsync), "",
                    "Failed to get snapshot in the new format, fallback to the legacy format");

                return await _legacyRepository.TryGetAsync();
            }

            return newResult;
        }
    }
}
