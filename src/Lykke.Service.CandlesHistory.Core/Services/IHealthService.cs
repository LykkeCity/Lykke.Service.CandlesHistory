using System;
using System.Collections.Generic;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

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
        void TraceCandlesBatchPersisted();

        IAssetPairRepositoryHealthService GetAssetPairRepositoryHealth(string repositoryKey);
        KeyValuePair<string, IAssetPairRepositoryHealthService>[] GetAssetPairRepositoriesHealth();
        void TraceSetPersistenceQueueState(int amountOfCandlesToDispatch);
    }
}