using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesManager : IStartable
    {
        Task ProcessQuoteAsync(IQuote quote);
        IEnumerable<IFeedCandle> GetCandles(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment);
    }
}