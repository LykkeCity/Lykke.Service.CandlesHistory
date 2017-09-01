using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ICandlesSubscriber _candlesSubcriber;
        private readonly IEnumerable<ISnapshotSerializer> _snapshotSerializers;
        private readonly ICandlesPersistenceQueue _persistenceQueue;
        private readonly ICandlesPersistenceManager _persistenceManager;
        private readonly object _lock;
        
        public ShutdownManager(
            ILog log,
            ICandlesSubscriber candlesSubscriber, 
            IEnumerable<ISnapshotSerializer> snapshotSerializers,
            ICandlesPersistenceQueue persistenceQueue,
            ICandlesPersistenceManager persistenceManager)
        {
            _log = log;
            _candlesSubcriber = candlesSubscriber;
            _snapshotSerializers = snapshotSerializers;
            _persistenceQueue = persistenceQueue;
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

                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Stopping candles subscriber...");
                
                _candlesSubcriber.Stop();

                await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Serializing state...");

                await Task.WhenAll(_snapshotSerializers.Select(s => s.SerializeAsync()));

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