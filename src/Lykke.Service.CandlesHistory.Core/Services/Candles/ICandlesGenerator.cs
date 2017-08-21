using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesGenerator
    {
        IFeedCandle GenerateCandle(IQuote quote, TimeInterval timeInterval);
    }
}