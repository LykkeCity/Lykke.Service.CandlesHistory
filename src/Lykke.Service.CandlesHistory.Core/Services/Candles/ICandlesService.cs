using System.Collections.Generic;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Domain;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesService
    {
        void InitializeHistory(IAssetPair assetPair, PriceType priceType, TimeInterval timeInterval, IEnumerable<IFeedCandle> candles);
        void AddQuote(IQuote quote, PriceType priceType, TimeInterval timeInterval);
    }
}