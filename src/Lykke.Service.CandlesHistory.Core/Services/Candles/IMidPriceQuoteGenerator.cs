using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface IMidPriceQuoteGenerator
    {
        IQuote TryGenerate(IQuote quote, int assetPairAccuracy);
    }
}