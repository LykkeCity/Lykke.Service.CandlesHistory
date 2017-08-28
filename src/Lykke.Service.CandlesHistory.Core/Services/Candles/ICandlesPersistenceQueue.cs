using System.Collections.Generic;
using Autofac;
using Common;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesPersistenceQueue : IStartable, IStopable
    {
        bool DispatchCandlesToPersist();
        void EnqueueCandle(IFeedCandle candle, string assetPairId, PriceType priceType, TimeInterval timeInterval);
        AssetPairCandle[] GetState();
        void SetState(AssetPairCandle[] state);
    }
}