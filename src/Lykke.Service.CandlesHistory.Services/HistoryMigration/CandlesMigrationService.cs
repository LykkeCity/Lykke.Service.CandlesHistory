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
        private readonly IMigrationProgressRepository _migrationProgressRepository;
        private readonly IFeedHistoryRepository _feedHistoryRepository;
        private readonly IFeedBidAskHistoryRepository _feedBidAskHistoryRepository;
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;

        public CandlesesMigrationService(
            IMigrationProgressRepository migrationProgressRepository,
            IFeedHistoryRepository feedHistoryRepository,
            IFeedBidAskHistoryRepository feedBidAskHistoryRepository,
            ICandlesHistoryRepository candlesHistoryRepository)
        {
            _migrationProgressRepository = migrationProgressRepository;
            _feedHistoryRepository = feedHistoryRepository;
            _feedBidAskHistoryRepository = feedBidAskHistoryRepository;
            _candlesHistoryRepository = candlesHistoryRepository;
        }

        public async Task<DateTime?> GetStartDateAsync(string assetPair, PriceType priceType)
        {
            var processedDate = await _migrationProgressRepository.GetProcessedDateAsync(assetPair, priceType);

            if (processedDate != null)
            {
                return processedDate.Value;
            }

            var oldestFeedHistory = await _feedHistoryRepository.GetTopRecordAsync(assetPair, priceType);

            return oldestFeedHistory
                ?.Candles
                .First()
                .ToCandle(assetPair, priceType, oldestFeedHistory.DateTime).Timestamp;
        }

        public async Task<DateTime> GetEndDateAsync(string assetPair, PriceType priceType, DateTime now)
        {
            var date = await _candlesHistoryRepository.GetFirstCandleDateTimeAsync(
                assetPair, 
                TimeInterval.Sec,
                priceType);

            // Sec candles packed into the minute rows, so round to the minute
            return date ?? now.RoundTo(TimeInterval.Sec);
        }

        public Task GetFeedHistoryCandlesByChunkAsync(string assetPair, PriceType priceType, DateTime startDate, DateTime endDate,
            Func<IEnumerable<IFeedHistory>, PriceType, Task> callback)
        {
            return _feedHistoryRepository.GetCandlesByChunkAsync(assetPair, priceType, startDate, endDate, callback);
        }

        //public Task GetFeedHistoryBidAskByChunkAsync(string assetPair, DateTime startDate, DateTime endDate,
        //    Func<IEnumerable<IFeedBidAskHistory>, Task> callback)
        //{
        //    return _feedBidAskHistoryRepository.GetHistoryByChunkAsync(assetPair, startDate, endDate, callback);
        //}

        //public async Task SaveBidAskHistoryAsync(string assetPair, IEnumerable<ICandle> candles, PriceType priceType)
        //{
        //    await _feedBidAskHistoryRepository.SaveHistoryItemAsync(assetPair, candles, priceType);
        //}

        //public async Task SetProcessedDateAsync(string assetPair, PriceType priceType, DateTime date)
        //{
        //    await _migrationProgressRepository.SetProcessedDateAsync(assetPair, priceType, date);
        //}

        //public async Task RemoveProcessedDateAsync(string assetPair)
        //{
        //    await _migrationProgressRepository.RemoveProcessedDateAsync(assetPair);
        //}
    }
}
