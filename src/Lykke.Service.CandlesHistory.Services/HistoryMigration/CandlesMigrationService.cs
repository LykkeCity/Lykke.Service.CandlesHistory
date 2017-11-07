using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    public class CandlesesMigrationService : ICandlesMigrationService
    {
        private readonly IFeedHistoryRepository _feedHistoryRepository;
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;

        public CandlesesMigrationService(
            IFeedHistoryRepository feedHistoryRepository,
            ICandlesHistoryRepository candlesHistoryRepository)
        {
            _feedHistoryRepository = feedHistoryRepository;
            _candlesHistoryRepository = candlesHistoryRepository;
        }

        public async Task<DateTime?> GetStartDateAsync(string assetPair, PriceType priceType)
        {
            var oldestFeedHistory = await _feedHistoryRepository.GetTopRecordAsync(assetPair, priceType);

            return oldestFeedHistory
                ?.Candles
                .First()
                .ToCandle(assetPair, priceType, oldestFeedHistory.DateTime).Timestamp;
        }

        public async Task<ICandle> GetEndCandleAsync(string assetPair, PriceType priceType)
        {
            var candle = await _candlesHistoryRepository.TryGetFirstCandleAsync(assetPair, TimeInterval.Sec, priceType);

            return candle;
        }

        public Task GetFeedHistoryCandlesByChunkAsync(string assetPair, PriceType priceType, DateTime startDate, DateTime endDate,
            Func<IEnumerable<IFeedHistory>, PriceType, Task> callback)
        {
            return _feedHistoryRepository.GetCandlesByChunkAsync(assetPair, priceType, startDate, endDate, callback);
        }
    }
}
