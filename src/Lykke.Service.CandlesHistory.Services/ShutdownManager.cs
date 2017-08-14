using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
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
        private readonly IEnumerable<IStopable> _stoppables;
        private readonly object _lock;
        
        public ShutdownManager(
            ILog log,
            ICandlesBroker broker, 
            ICandlesPersistenceQueue persistenceQueue,
            IEnumerable<IStopable> stoppables)
        {
            _log = log;
            _broker = broker;
            _persistenceQueue = persistenceQueue;
            _stoppables = stoppables;

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
                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(Shutdown), "", "Shutting down...");

                _broker.Stop();

                // Dispatch all generated candles to batches
                while (_persistenceQueue.CandlesToDispatchQueueLength > 0)
                {
                    _persistenceQueue.DispatchCandlesToPersist();
                }

                // Wait until all batches is persisted
                _persistenceQueue.Stop();

                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(Shutdown), "", "Persistence queue is stopped, stopping rest of services...");

                // Let all stoppables to be stopped
                foreach (var stoppable in _stoppables)
                {
                    stoppable.Stop();
                }

                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(Shutdown), "", "Shutted down");

                IsShuttedDown = true;
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(ShutdownManager), nameof(Shutdown), "", ex);
                throw;
            }
            finally
            {
                IsShuttingDown = false;
            }
        }
    }
}