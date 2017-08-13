using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services
{
    public class ShutdownManager : IShutdownManager
    {
        public bool IsShuttedDown { get; private set; }
        public bool IsShuttingDown { get; private set; }
        
        private readonly ILog _log;
        private readonly ICandlesBroker _broker;
        private readonly ICandlesPersistenceQueue _persistenceQueue;
        private readonly object _lock;
        
        public ShutdownManager(
            ILog log,
            ICandlesBroker broker, 
            ICandlesPersistenceQueue persistenceQueue)
        {
            _log = log;
            _broker = broker;
            _persistenceQueue = persistenceQueue;

            _lock = new object();
        }

        public async Task Shutdown()
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
                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(Shutdown), "", "Shutdown is started");

                _broker.Stop();

                // Dispatch all generated candles to batches
                while (_persistenceQueue.CandlesToDispatchQueueLength > 0)
                {
                    _persistenceQueue.DispatchCandlesToPersist();
                }

                // Wait until all batches is persisted
                _persistenceQueue.Stop();

                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(Shutdown), "", "Shutdown is ended");

                IsShuttedDown = true;
            }
            finally
            {
                IsShuttingDown = false;
            }
        }
    }
}