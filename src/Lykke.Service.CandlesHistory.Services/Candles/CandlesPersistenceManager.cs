using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesPersistenceManager : TimerPeriod
    {
        private readonly ICandlesPersistenceQueue _persistenceQueue;

        public CandlesPersistenceManager(
            ICandlesPersistenceQueue persistenceQueue,
            ILog log) : 

            base(nameof(CandlesPersistenceManager), (int)TimeSpan.FromSeconds(10).TotalMilliseconds, log)
        {
            _persistenceQueue = persistenceQueue;
        }

        public override Task Execute()
        {
            _persistenceQueue.Persist();

            return Task.FromResult(0);
        }
    }
}