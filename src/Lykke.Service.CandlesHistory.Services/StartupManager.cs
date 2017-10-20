using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly ILog _log;
        private readonly ICandlesSubscriber _candlesSubscriber;
        private readonly SnapshotSerializer<IImmutableDictionary<string, IImmutableList<ICandle>>> _candlesCacheSnapshotSerializer;
        private readonly SnapshotSerializer<IImmutableList<ICandle>> _persistenceQueueSnapshotSerializer;
        private readonly ICandlesPersistenceQueue _persistenceQueue;
        private readonly ICandlesPersistenceManager _persistenceManager;
        private readonly ICandlesCacheInitalizationService _cacheInitalizationService;

        public StartupManager(
            ILog log, 
            ICandlesCacheInitalizationService cacheInitalizationService,
            ICandlesSubscriber candlesSubscriber,
            SnapshotSerializer<IImmutableDictionary<string, IImmutableList<ICandle>>> candlesCacheSnapshotSerializer,
            SnapshotSerializer<IImmutableList<ICandle>> persistenceQueueSnapshotSerializer,
            ICandlesPersistenceQueue persistenceQueue,
            ICandlesPersistenceManager persistenceManager)
        {
            _log = log.CreateComponentScope(nameof(StartupManager));
            _candlesSubscriber = candlesSubscriber;
            _candlesCacheSnapshotSerializer = candlesCacheSnapshotSerializer;
            _persistenceQueueSnapshotSerializer = persistenceQueueSnapshotSerializer;
            _persistenceQueue = persistenceQueue;
            _persistenceManager = persistenceManager;
            _cacheInitalizationService = cacheInitalizationService;
        }

        public async Task StartAsync()
        {
            // TODO: Migration candles generator snapshot loading

            // TODO: Continue migrations which were in progress when app was shutted down

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Deserializing persistence queue async...");

            var tasks = new List<Task>
            {
                _persistenceQueueSnapshotSerializer.DeserializeAsync()
            };

            //await _log.WriteInfoAsync(nameof(StartAsync), "", "Deserializing cache...");

            //if (!await _candlesCacheSnapshotSerializer.DeserializeAsync())
            //{
            //    await _log.WriteInfoAsync(nameof(StartAsync), "", "Initializing cache from history async...");

            //    tasks.Add(_cacheInitalizationService.InitializeCacheAsync());
            //}

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Waiting for async tasks...");

            await Task.WhenAll(tasks);

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Starting persistence queue...");

            _persistenceQueue.Start();

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Starting persistence manager...");

            _persistenceManager.Start();

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Starting candles subscriber...");

            _candlesSubscriber.Start();

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Started up");
        }
    }
}
