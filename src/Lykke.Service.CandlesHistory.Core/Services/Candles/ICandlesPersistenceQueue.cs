using Autofac;
using Common;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesPersistenceQueue : IStartable, IStopable
    {
        int BatchesToPersistQueueLength { get; }
        int CandlesToDispatchQueueLength { get; }

        void DispatchCandlesToPersist();
        void EnqueCandle(IFeedCandle candle, string assetPairId, PriceType priceType, TimeInterval timeInterval);
    }
}