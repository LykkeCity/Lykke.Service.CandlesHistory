using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services
{
    public class HealthService : IHealthService
    {
        public TimeSpan AveragePersistTime => _totalPersistCount != 0
            ? new TimeSpan(_totalPersistTime.Ticks / _totalPersistCount)
            : TimeSpan.Zero;
        public TimeSpan TotalPersistTime => _totalPersistTime;
        public int AverageCandlesPersistedPerSecond => TotalPersistTime != TimeSpan.Zero 
            ? (int)(_totalCandlesPersistedCount / TotalPersistTime.TotalSeconds)
            : 0;
        public long TotalCandlesPersistedCount => _totalCandlesPersistedCount;
        public int BatchesToPersistQueueLength => _batchesToPersistQueueLength;
        public int CandlesToDispatchQueueLength => _candlesToDispatchQueueLength;

        private long _totalCandlesPersistedCount;
        private int _batchesToPersistQueueLength;
        private int _candlesToDispatchQueueLength;
        private Stopwatch _persistCandlesStopwatch;
        private TimeSpan _totalPersistTime;
        private long _totalPersistCount;

        private readonly ConcurrentDictionary<string, IAssetPairRepositoryHealthService> _assetPairRepositoryHealthServices;

        public HealthService()
        {
            _assetPairRepositoryHealthServices = new ConcurrentDictionary<string, IAssetPairRepositoryHealthService>();
        }

        public void TraceStartPersistCandles(int candlesCount)
        {
            if (_persistCandlesStopwatch != null)
            {
                return;
            }

            _totalCandlesPersistedCount += candlesCount;

            _persistCandlesStopwatch = Stopwatch.StartNew();
        }

        public void TraceStopPersistCandles()
        {
            _persistCandlesStopwatch.Stop();

            _totalPersistTime += _persistCandlesStopwatch.Elapsed;
            ++_totalPersistCount;

            _persistCandlesStopwatch = null;
        }

        public void TraceEnqueueCandle()
        {
            Interlocked.Increment(ref _candlesToDispatchQueueLength);
        }

        public void TraceCandlesBatchDispatched(int candlesCount)
        {
            Interlocked.Add(ref _candlesToDispatchQueueLength, -candlesCount);
            Interlocked.Increment(ref _batchesToPersistQueueLength);
        }

        public void TraceCandlesBatchPersisted()
        {
            Interlocked.Decrement(ref _batchesToPersistQueueLength);
        }

        public IAssetPairRepositoryHealthService GetAssetPairRepositoryHealth(string repositoryKey)
        {
            return _assetPairRepositoryHealthServices.GetOrAdd(
                repositoryKey,
                key => new AssetPairRepositoryHealthService());
        }

        public KeyValuePair<string, IAssetPairRepositoryHealthService>[] GetAssetPairRepositoriesHealth()
        {
            return _assetPairRepositoryHealthServices.ToArray();
        }
    }
}