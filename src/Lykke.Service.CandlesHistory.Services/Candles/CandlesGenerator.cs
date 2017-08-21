using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesGenerator : ICandlesGenerator
    {
        public IFeedCandle GenerateCandle(IQuote quote, TimeInterval timeInterval)
        {
            var intervalDate = quote.Timestamp.RoundTo(timeInterval);

            return quote.ToCandle(intervalDate);
        }
    }
}