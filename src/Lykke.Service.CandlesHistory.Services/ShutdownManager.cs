using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.HistoryMigration;

namespace Lykke.Service.CandlesHistory.Services
{
    public class ShutdownManager : IShutdownManager
    {
        public bool IsShuttedDown { get; private set; }
        public bool IsShuttingDown { get; private set; }
        
        private readonly ILog _log;
        private readonly ICandlesSubscriber _candlesSubcriber;
        private readonly ISnapshotSerializer _snapshotSerializer;
        private readonly ICandlesCacheSnapshotRepository _candlesCacheSnapshotRepository;
        private readonly ICandlesPersistenceQueueSnapshotRepository _persistenceQueueSnapshotRepository;
        private readonly IMissedCandlesGeneratorSnapshotRepository _missedCandlesGeneratorSnapshotRepository;
        private readonly IMigrationCandlesGeneratorSnapshotRepository _migrationCandlesGeneratorSnapshotRepository;
        private readonly ICandlesCacheService _candlesCacheService;
        private readonly ICandlesPersistenceQueue _persistenceQueue;
        private readonly MissedCandlesGenerator _missedCandlesGenerator;
        private readonly MigrationCandlesGenerator _migrationCandlesGenerator;
        private readonly ICandlesPersistenceManager _persistenceManager;
        private readonly CandlesMigrationManager _migrationManager;

        public ShutdownManager(
            ILog log,
            ICandlesSubscriber candlesSubscriber, 
            ISnapshotSerializer snapshotSerializer,
            ICandlesCacheSnapshotRepository candlesCacheSnapshotRepository,
            ICandlesPersistenceQueueSnapshotRepository persistenceQueueSnapshotRepository,
            IMissedCandlesGeneratorSnapshotRepository missedCandlesGeneratorSnapshotRepository,
            IMigrationCandlesGeneratorSnapshotRepository migrationCandlesGeneratorSnapshotRepository,
            ICandlesCacheService candlesCacheService,
            ICandlesPersistenceQueue persistenceQueue,
            MissedCandlesGenerator missedCandlesGenerator,
            MigrationCandlesGenerator migrationCandlesGenerator,
            ICandlesPersistenceManager persistenceManager,
            CandlesMigrationManager migrationManager)
        {
            _log = log.CreateComponentScope(nameof(ShutdownManager));
            _candlesSubcriber = candlesSubscriber;
            _snapshotSerializer = snapshotSerializer;
            _candlesCacheSnapshotRepository = candlesCacheSnapshotRepository;
            _persistenceQueueSnapshotRepository = persistenceQueueSnapshotRepository;
            _missedCandlesGeneratorSnapshotRepository = missedCandlesGeneratorSnapshotRepository;
            _migrationCandlesGeneratorSnapshotRepository = migrationCandlesGeneratorSnapshotRepository;
            _candlesCacheService = candlesCacheService;
            _persistenceQueue = persistenceQueue;
            _missedCandlesGenerator = missedCandlesGenerator;
            _migrationCandlesGenerator = migrationCandlesGenerator;
            _persistenceManager = persistenceManager;
            _migrationManager = migrationManager;
        }

        public async Task ShutdownAsync()
        {
            IsShuttingDown = true;

            await _log.WriteInfoAsync(nameof(ShutdownAsync), "", "Stopping persistence manager...");
                
            _persistenceManager.Stop();

            await _log.WriteInfoAsync(nameof(ShutdownAsync), "", "Stopping persistence queue...");
                
            _persistenceQueue.Stop();

            await _log.WriteInfoAsync(nameof(ShutdownAsync), "", "Stopping candles subscriber...");
                
            _candlesSubcriber.Stop();

            await _log.WriteInfoAsync(nameof(ShutdownAsync), "", "Stopping candles migration manager...");

            _migrationManager.Stop();

            await _log.WriteInfoAsync(nameof(ShutdownAsync), "", "Serializing state...");

            await Task.WhenAll(
                _snapshotSerializer.SerializeAsync(_persistenceQueue, _persistenceQueueSnapshotRepository),
                _snapshotSerializer.SerializeAsync(_candlesCacheService, _candlesCacheSnapshotRepository));
            // TODO: Uncomment if needed
                //_snapshotSerializer.SerializeAsync(_missedCandlesGenerator, _missedCandlesGeneratorSnapshotRepository),
                //_snapshotSerializer.SerializeAsync(_migrationCandlesGenerator, _migrationCandlesGeneratorSnapshotRepository));

            await _log.WriteInfoAsync(nameof(ShutdownAsync), "", "Shutted down");

            IsShuttedDown = true;
            IsShuttingDown = false;
        }
    }
}
