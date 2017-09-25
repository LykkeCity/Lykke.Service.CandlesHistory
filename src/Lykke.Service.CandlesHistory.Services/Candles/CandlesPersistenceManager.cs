using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Settings;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesPersistenceManager : 
        TimerPeriod,
        ICandlesPersistenceManager
    {
        private readonly ICandlesPersistenceQueue _persistenceQueue;

        public CandlesPersistenceManager(
            ICandlesPersistenceQueue persistenceQueue,
            ILog log,
            PersistenceSettings settings) : 

            base(nameof(CandlesPersistenceManager), (int)settings.PersistPeriod.TotalMilliseconds, log)
        {
            _persistenceQueue = persistenceQueue;
        }

        public override Task Execute()
        {
            _persistenceQueue.DispatchCandlesToPersist();

            return Task.FromResult(0);
        }
    }
}