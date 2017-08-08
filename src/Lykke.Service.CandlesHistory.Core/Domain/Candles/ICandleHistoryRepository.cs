using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public interface ICandleHistoryRepository
    {
        Task InsertOrMergeAsync(IEnumerable<IFeedCandle> candles, string assetPairId, TimeInterval interval, PriceType priceType);
        Task<IEnumerable<IFeedCandle>> GetCandlesAsync(string assetPairId, TimeInterval interval, PriceType priceType, DateTime from, DateTime to);
    }
}