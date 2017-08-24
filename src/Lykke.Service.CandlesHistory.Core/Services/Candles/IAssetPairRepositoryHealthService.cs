namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface IAssetPairRepositoryHealthService
    {
        double AverageRowMergeGroupsCount { get; }
        double AverageRowMergeCandlesCount { get; }

        void TraceRowMergedGroupsCount(int groupsCount);
        void TraceRowMergedCandlesCount(int candlesCount);
    }
}