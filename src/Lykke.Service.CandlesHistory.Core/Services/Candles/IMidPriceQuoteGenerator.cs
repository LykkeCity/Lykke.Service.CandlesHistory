﻿using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Domain;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface IMidPriceQuoteGenerator
    {
        IQuote TryGenerate(IQuote quote, IAssetPair assetPair);
    }
}