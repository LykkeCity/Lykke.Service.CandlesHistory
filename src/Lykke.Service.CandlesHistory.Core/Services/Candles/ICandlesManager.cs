﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesManager
    {
        Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment, SlotType? activeSlot = null);

        Task<ICandle> TryGetOldestCandleAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval interval);

        Task<SlotType> GetActiveSlotAsync();
    }
}
