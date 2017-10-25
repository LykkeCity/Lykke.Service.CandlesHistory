using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Settings;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesPersistenceManager : 
        TimerPeriod,
        ICandlesPersistenceManager
    {
        private readonly ICandlesPersistenceQueue _persistenceQueue;
        private readonly IHealthService _healthService;
        private readonly PersistenceSettings _settings;
        private DateTime _lastDispatchMoment;

        public CandlesPersistenceManager(
            ICandlesPersistenceQueue persistenceQueue,
            IHealthService healthService,
            ILog log,
            PersistenceSettings settings) : 

            base(nameof(CandlesPersistenceManager), (int)TimeSpan.FromSeconds(5).TotalMilliseconds, log)
        {
            _persistenceQueue = persistenceQueue;
            _healthService = healthService;
            _settings = settings;

            _lastDispatchMoment = DateTime.MinValue;
        }

        public override Task Execute()
        {
            var now = DateTime.UtcNow;

            if (_healthService.CandlesToDispatchQueueLength > 100000 ||
                now - _lastDispatchMoment > _settings.PersistPeriod)
            {
                if (_healthService.BatchesToPersistQueueLength < 10)
                {
                    _persistenceQueue.DispatchCandlesToPersist();
                    _lastDispatchMoment = now;
                }
            }


            return Task.FromResult(0);
        }
    }
}
