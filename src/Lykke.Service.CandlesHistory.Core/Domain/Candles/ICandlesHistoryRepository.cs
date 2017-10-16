using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public interface ICandlesHistoryRepository
    {
        Task InsertOrMergeAsync(IReadOnlyCollection<ICandle> candles, string assetPairId, PriceType priceType, TimeInterval timeInterval);
        Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, TimeInterval interval, PriceType priceType, DateTime from, DateTime to);
        bool CanStoreAssetPair(string assetPairId);
        Task<DateTime?> GetTopRecordDateTimeAsync(string assetPairId, TimeInterval interval);
    }
}