using System;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.Assets.Client.Custom;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface IMidPriceQuoteGenerator
    {
        void Initialize(string assetPairId, bool isBid, double price, DateTime timestamp);
        IQuote TryGenerate(IQuote quote, int assetPairAccuracy);
    }
}