using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class ShutdownManager : IShutdownManager
    {
        public bool IsShuttedDown { get; private set; }
        public bool IsShuttingDown { get; private set; }
        
        private readonly ILog _log;
        private readonly ICandlesBroker _broker;
        private readonly ICandlesPersistenceQueue _persistenceQueue;
        private readonly ICandlesCacheSerializationService _cacheSerializationService;
        private readonly ICandlesPersistenceQueueSerializationService _persistenceQueueSerializationService;
        private readonly ICandlesPersistenceManager _persistenceManager;
        private readonly object _lock;
        
        public ShutdownManager(
            ILog log,
            ICandlesBroker broker, 
            ICandlesPersistenceQueue persistenceQueue,
            ICandlesCacheSerializationService cacheSerializationService,
            ICandlesPersistenceQueueSerializationService persistenceQueueSerializationService,
            ICandlesPersistenceManager persistenceManager)
        {
            _log = log;
            _broker = broker;
            _persistenceQueue = persistenceQueue;
            _cacheSerializationService = cacheSerializationService;
            _persistenceQueueSerializationService = persistenceQueueSerializationService;
            _persistenceManager = persistenceManager;

            _lock = new object();
        }

        public async Task ShutdownAsync()
        {
            if (IsShuttedDown)
            {
                return;
            }

            lock (_lock)
            {
                if (IsShuttedDown)
                {
                    return;
                }

                while (IsShuttingDown)
                {
                    Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
                }

                if (IsShuttedDown)
                {
                    return;
                }

                IsShuttingDown = true;
            }

            try
            {
                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Stopping persistence manager...");
                
                _persistenceManager.Stop();

                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Stopping persistence queue...");
                
                _persistenceQueue.Stop();

                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Stopping broker...");
                
                _broker.Stop();

                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Serializing state...");

                await Task.WhenAll(
                    _persistenceQueueSerializationService.SerializeQueueAsync(),
                    _cacheSerializationService.SerializeCacheAsync());

                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Shutted down");

                IsShuttedDown = true;
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", ex);
                throw;
            }
            finally
            {
                IsShuttingDown = false;
            }
        }
    }
}