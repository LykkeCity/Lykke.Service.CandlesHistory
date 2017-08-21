using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesGenerator
    {
        IFeedCandle GenerateAskBidCandle(IQuote quote, TimeInterval timeInterval);
        IFeedCandle GenerateMidCandle(IFeedCandle askCandle, IFeedCandle bidCandle, int assetPairAccuracy);
    }
}