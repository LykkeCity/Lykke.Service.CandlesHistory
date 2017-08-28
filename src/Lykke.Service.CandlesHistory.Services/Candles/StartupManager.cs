using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class StartupManager : IStartupManager
    {
        private readonly ILog _log;
        private readonly ICandlesBroker _broker;
        private readonly ICandlesPersistenceQueue _persistenceQueue;
        private readonly ICandlesPersistenceManager _persistenceManager;
        private readonly ICandlesCacheInitalizationService _cacheInitalizationService;
        private readonly ICandlesCacheDeserializationService _cacheDeserializationService;
        private readonly ICandlesPersistenceQueueDeserializationService _persistenceQueueDeserializationService;

        public StartupManager(
            ILog log, 
            ICandlesCacheInitalizationService cacheInitalizationService,
            ICandlesCacheDeserializationService cacheDeserializationService,
            ICandlesPersistenceQueueDeserializationService persistenceQueueDeserializationService,
            ICandlesBroker broker,
            ICandlesPersistenceQueue persistenceQueue,
            ICandlesPersistenceManager persistenceManager)
        {
            _log = log;
            _broker = broker;
            _persistenceQueue = persistenceQueue;
            _persistenceManager = persistenceManager;
            _cacheInitalizationService = cacheInitalizationService;
            _cacheDeserializationService = cacheDeserializationService;
            _persistenceQueueDeserializationService = persistenceQueueDeserializationService;
        }

        public async Task StartAsync()
        {
            try
            {
                await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "",
                    "Deserializing persistence queue async...");

                var tasks = new List<Task>
                {
                    _persistenceQueueDeserializationService.DeserializeQueueAsync()
                };

                await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Deserializing cache...");

                if (!await _cacheDeserializationService.DeserializeCacheAsync())
                {
                    await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Initializing cache from history async...");

                    tasks.Add(_cacheInitalizationService.InitializeCacheAsync());
                }

                await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Waiting for async tasks...");

                await Task.WhenAll(tasks);

                await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Starting persistence queue...");

                _persistenceQueue.Start();

                await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Starting persistence manager...");

                _persistenceManager.Start();

                await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Starting broker...");

                _broker.Start();

                await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Started up");
            }
            catch (Exception ex)
            {
                await _log.WriteFatalErrorAsync(nameof(StartupManager), nameof(StartAsync), "", ex);
            }
        }
    }
}