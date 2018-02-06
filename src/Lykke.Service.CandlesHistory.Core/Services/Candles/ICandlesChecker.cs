namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesChecker
    {
        bool CanHandleAssetPair(string assetPairId);
    }
}
