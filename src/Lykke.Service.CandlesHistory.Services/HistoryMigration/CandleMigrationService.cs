using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    public class CandleMigrationService
    {
        private readonly IMigrationProgressRepository _migrationProgressRepository;
        private readonly IFeedHistoryRepository _feedHistoryRepository;
        private readonly IFeedBidAskHistoryRepository _feedBidAskHistoryRepository;
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;

        public CandleMigrationService(
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

            var oldestFeedHistory = await _feedHistoryRepository.GetTopRecordAsync(assetPair);

            return oldestFeedHistory?.DateTime;
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

        public async Task<IFeedHistory> GetFeedHistoryItemAsync(string assetPair, PriceType priceType, string rowKey)
        {
            return await _feedHistoryRepository.GetCandle(assetPair, priceType, rowKey);
        }

        public Task GetFeedHistoryCandlesByChunkAsync(string assetPair, PriceType priceType, DateTime startDate, DateTime endDate,
            Func<IEnumerable<IFeedHistory>, PriceType, Task> callback)
        {
            return _feedHistoryRepository.GetCandlesByChunkAsync(assetPair, priceType, startDate, endDate, callback);
        }

        public Task GetFeedHistoryBidAskByChunkAsync(string assetPair, DateTime startDate, DateTime endDate,
            Func<IEnumerable<IFeedBidAskHistory>, Task> callback)
        {
            return _feedBidAskHistoryRepository.GetHistoryByChunkAsync(assetPair, startDate, endDate, callback);
        }

        public async Task SaveBidAskHistoryAsync(string assetPair, IEnumerable<ICandle> candles, PriceType priceType)
        {
            var grouppedCandles = candles.GroupBy(c => c.Timestamp.RoundToMinute());

            switch (priceType)
            {
                case PriceType.Bid:
                    foreach (var minuteGroup in grouppedCandles)
                    {
                        await _feedBidAskHistoryRepository.SaveHistoryItemAsync(assetPair, minuteGroup.Key, null, minuteGroup);

                    }
                    break;

                case PriceType.Ask:
                    foreach (var minuteGroup in grouppedCandles)
                    {
                        await _feedBidAskHistoryRepository.SaveHistoryItemAsync(assetPair, minuteGroup.Key, minuteGroup, null);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(priceType), priceType, "Only ask or bid price types are available");
            }
        }

        public async Task SetProcessedDateAsync(string assetPair, PriceType priceType, DateTime date)
        {
            await _migrationProgressRepository.SetProcessedDateAsync(assetPair, priceType, date);
        }

        public async Task RemoveProcessedDateAsync(string assetPair)
        {
            await _migrationProgressRepository.RemoveProcessedDateAsync(assetPair);
        }
    }
}
