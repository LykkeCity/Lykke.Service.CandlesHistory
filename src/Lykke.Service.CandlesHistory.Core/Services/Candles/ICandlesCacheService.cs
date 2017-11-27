using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesCacheService : IHaveState<IImmutableDictionary<string, IImmutableList<ICandle>>>
    {
        void Initialize(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, IReadOnlyCollection<ICandle> candles);
        void Cache(ICandle candle);
        IEnumerable<ICandle> GetCandles(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime? toMoment);
    }
}
