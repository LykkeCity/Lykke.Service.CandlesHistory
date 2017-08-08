using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesPersistenceQueue
    {
        int PersistTasksQueueLength { get; }
        int CandlesToPersistQueueLength { get; }

        void Persist();
        void EnqueCandle(IFeedCandle candle, string assetPairId, PriceType priceType, TimeInterval timeInterval);
    }
}