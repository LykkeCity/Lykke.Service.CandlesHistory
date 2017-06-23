using System;
using System.Collections.Generic;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICachedCandlesHistoryService
    {
        void InitializeHistory(string assetPairId, PriceType priceType, TimeInterval timeInterval, IEnumerable<IFeedCandle> candles);
        void AddQuote(IQuote quote, PriceType priceType, TimeInterval timeInterval);
        IEnumerable<IFeedCandle> GetCandles(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment);
    }
}