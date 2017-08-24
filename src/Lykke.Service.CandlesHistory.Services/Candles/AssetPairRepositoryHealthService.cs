using System.Threading;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class AssetPairRepositoryHealthService : IAssetPairRepositoryHealthService
    {
        public double AverageRowMergeGroupsCount => _totalRowMergeGroupsCount / (double)_totalRowMergeGroupsExecutionsCount;
        public double AverageRowMergeCandlesCount => _totalRowMergeCandlesCount / (double) _totalRowMergeCandlesExecutionsCount;

        private long _totalRowMergeGroupsCount;
        private long _totalRowMergeGroupsExecutionsCount;
        private long _totalRowMergeCandlesCount;
        private long _totalRowMergeCandlesExecutionsCount;
        
        public void TraceRowMergedGroupsCount(int groupsCount)
        {
            Interlocked.Add(ref _totalRowMergeGroupsCount, groupsCount);
            Interlocked.Increment(ref _totalRowMergeGroupsExecutionsCount);
        }

        public void TraceRowMergedCandlesCount(int candlesCount)
        {
            Interlocked.Add(ref _totalRowMergeCandlesCount, candlesCount);
            Interlocked.Increment(ref _totalRowMergeCandlesExecutionsCount);
        }
    }
}