﻿using System;
using System.Collections.Generic;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesCacheService
    {
        void InitializeHistory(string assetPairId, TimeInterval timeInterval, PriceType priceType, IEnumerable<IFeedCandle> candles);
        IFeedCandle AddQuote(IQuote quote, PriceType priceType, TimeInterval timeInterval);
        IEnumerable<IFeedCandle> GetCandles(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment);
        IFeedCandle GetMidPriceCandle(string assetPair, int assetPairAccuracy, DateTime timestamp, TimeInterval timeInterval);
    }
}