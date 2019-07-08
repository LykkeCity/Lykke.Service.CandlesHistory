// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Contract;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public interface ICandlesHistoryRepository
    {
        Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, CandleTimeInterval interval, CandlePriceType priceType, DateTime @from, DateTime to);
        Task<ICandle> TryGetFirstCandleAsync(string assetPairId, CandleTimeInterval interval, CandlePriceType priceType);
    }
}
