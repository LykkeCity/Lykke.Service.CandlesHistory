using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.HistoryMigration;

namespace Lykke.Service.CandlesHistory.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly ILog _log;
        private readonly ICandlesSubscriber _candlesSubscriber;
        private readonly ISnapshotSerializer _snapshotSerializer;
        private readonly ICandlesCacheSnapshotRepository _candlesCacheSnapshotRepository;
        private readonly ICandlesPersistenceQueueSnapshotRepository _persistenceQueueSnapshotRepository;
        private readonly IMissedCandlesGeneratorSnapshotRepository _missedCandlesGeneratorSnapshotRepository;
        private readonly IMigrationCandlesGeneratorSnapshotRepository _migrationCandlesGeneratorSnapshotRepository;
        private readonly ICandlesCacheService _candlesCacheService;
        private readonly MissedCandlesGenerator _missedCandlesGenerator;
        private readonly MigrationCandlesGenerator _migrationCandlesGenerator;
        private readonly ICandlesPersistenceQueue _persistenceQueue;
        private readonly ICandlesPersistenceManager _persistenceManager;
        private readonly CandlesMigrationManager _candlesMigrationManager;
        private readonly ICandlesCacheInitalizationService _cacheInitalizationService;

        public StartupManager(
            ILog log, 
            ICandlesCacheInitalizationService cacheInitalizationService,
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
            CandlesMigrationManager candlesMigrationManager)
        {
            _log = log.CreateComponentScope(nameof(StartupManager));
            _candlesSubscriber = candlesSubscriber;
            _snapshotSerializer = snapshotSerializer;
            _candlesCacheSnapshotRepository = candlesCacheSnapshotRepository;
            _persistenceQueueSnapshotRepository = persistenceQueueSnapshotRepository;
            _missedCandlesGeneratorSnapshotRepository = missedCandlesGeneratorSnapshotRepository;
            _migrationCandlesGeneratorSnapshotRepository = migrationCandlesGeneratorSnapshotRepository;
            _candlesCacheService = candlesCacheService;
            _missedCandlesGenerator = missedCandlesGenerator;
            _migrationCandlesGenerator = migrationCandlesGenerator;
            _persistenceQueue = persistenceQueue;
            _persistenceManager = persistenceManager;
            _candlesMigrationManager = candlesMigrationManager;
            _cacheInitalizationService = cacheInitalizationService;
        }

        public async Task StartAsync()
        {
            // TODO: Continue migrations which were in progress when app was shutted down

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Deserializing persistence queue async...");
            await _log.WriteInfoAsync(nameof(StartAsync), "", "Deserializing missed candles generator state async...");
            await _log.WriteInfoAsync(nameof(StartAsync), "", "Deserializing migration candles generator state async...");
            
            var tasks = new List<Task>
            {
                _snapshotSerializer.DeserializeAsync(_persistenceQueue, _persistenceQueueSnapshotRepository),
                // TODO: Uncomment if needed
                //_snapshotSerializer.DeserializeAsync(_missedCandlesGenerator, _missedCandlesGeneratorSnapshotRepository),
                //_snapshotSerializer.DeserializeAsync(_migrationCandlesGenerator, _migrationCandlesGeneratorSnapshotRepository)
            };

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Deserializing cache...");

            if (!await _snapshotSerializer.DeserializeAsync(_candlesCacheService, _candlesCacheSnapshotRepository))
            {
                await _log.WriteInfoAsync(nameof(StartAsync), "", "Initializing cache from the history async...");

                tasks.Add(_cacheInitalizationService.InitializeCacheAsync());
            }

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Waiting for async tasks...");

            await Task.WhenAll(tasks);

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Starting persistence queue...");

            _persistenceQueue.Start();

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Starting persistence manager...");

            _persistenceManager.Start();

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Starting candles subscriber...");

            _candlesSubscriber.Start();

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Resuming history migration...");

            _candlesMigrationManager.Resume();

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Started up");
        }
    }
}
