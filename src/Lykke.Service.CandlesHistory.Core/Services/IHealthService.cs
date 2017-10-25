using System;

namespace Lykke.Service.CandlesHistory.Core.Services
{
    public interface IHealthService
    {
        TimeSpan AveragePersistTime { get; }
        TimeSpan TotalPersistTime { get; }

        int BatchesToPersistQueueLength { get; }
        int CandlesToDispatchQueueLength { get; }
        int AverageCandlesPersistedPerSecond { get; }
        long TotalCandlesPersistedCount { get; }

        void TraceStartPersistCandles(int candlesCount);
        void TraceStopPersistCandles();
     
        void TraceEnqueueCandle();
        void TraceCandlesBatchDispatched(int candlesCount);
        void TraceCandlesBatchPersisted(int candlesCount);

        void TraceSetPersistenceQueueState(int amountOfCandlesToDispatch);
    }
}
