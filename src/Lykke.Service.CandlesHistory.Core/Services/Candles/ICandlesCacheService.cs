using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesCacheService : IHaveState<IImmutableDictionary<string, IImmutableList<ICandle>>>
    {
        void Initialize(string assetPairId, PriceType priceType, TimeInterval timeInterval, IReadOnlyCollection<ICandle> candles);
        void Cache(ICandle candle);
        IEnumerable<ICandle> GetCandles(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment);
    }
}