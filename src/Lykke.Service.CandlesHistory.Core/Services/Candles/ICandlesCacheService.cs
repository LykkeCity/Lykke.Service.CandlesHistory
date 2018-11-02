using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesCacheService : IHaveState<IImmutableDictionary<string, IImmutableList<ICandle>>>
    {
        Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment, SlotType activeSlot);
        SlotType GetActiveSlot();
    }
}
