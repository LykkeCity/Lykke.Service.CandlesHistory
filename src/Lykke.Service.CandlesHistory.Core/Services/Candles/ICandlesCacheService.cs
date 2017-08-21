using System;
using System.Collections.Generic;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesCacheService
    {
        void InitializeHistory(string assetPairId, TimeInterval timeInterval, PriceType priceType, IEnumerable<IFeedCandle> candles);
        void AddCandle(IFeedCandle candle, string assetPairId, PriceType priceType, TimeInterval timeInterval);
        IEnumerable<IFeedCandle> GetCandles(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment);
    }
}