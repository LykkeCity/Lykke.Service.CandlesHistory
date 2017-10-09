using System.Collections.Immutable;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using Lykke.Service.CandleHistory.Repositories.Snapshots.Legacy;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandleHistory.Repositories.Snapshots.Migration
{
    public class CandlesCacheSnapshotMigrationRepository : ISnapshotRepository<IImmutableDictionary<string, IImmutableList<ICandle>>>
    {
        private readonly ILog _log;
        private readonly LegacyCandlesCacheSnapshotRepository _legacyRepository;
        private readonly CandlesCacheSnapshotRepository _repository;

        public CandlesCacheSnapshotMigrationRepository(IBlobStorage storage, ILog log)
        {
            _log = log;
            _legacyRepository = new LegacyCandlesCacheSnapshotRepository(storage);
            _repository = new CandlesCacheSnapshotRepository(storage);
        }

        public Task SaveAsync(IImmutableDictionary<string, IImmutableList<ICandle>> state)
        {
            return _repository.SaveAsync(state);
        }

        public async Task<IImmutableDictionary<string, IImmutableList<ICandle>>> TryGetAsync()
        {
            var newResult = await _repository.TryGetAsync();
            if (newResult == null)
            {
                await _log.WriteWarningAsync(nameof(CandlesCacheSnapshotMigrationRepository), nameof(TryGetAsync), "",
                    "Failed to get snapshot in the new format, fallback to the legacy format");

                return await _legacyRepository.TryGetAsync();
            }

            return newResult;
        }
    }
}
