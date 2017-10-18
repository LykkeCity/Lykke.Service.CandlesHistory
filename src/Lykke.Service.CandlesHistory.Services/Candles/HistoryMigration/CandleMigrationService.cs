using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Extensions;

namespace Lykke.Service.CandlesHistory.Services.Candles.HistoryMigration
{
    public class CandleMigrationService
    {
        private readonly IProcessedCandlesRepository _processedCandlesRepository;
        private readonly IFeedHistoryRepository _feedHistoryRepository;
        private readonly IFeedBidAskHistoryRepository _feedBidAskHistoryRepository;
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;

        public CandleMigrationService(
            IProcessedCandlesRepository processedCandlesRepository,
            IFeedHistoryRepository feedHistoryRepository,
            IFeedBidAskHistoryRepository feedBidAskHistoryRepository,
            ICandlesHistoryRepository candlesHistoryRepository)
        {
            _processedCandlesRepository = processedCandlesRepository;
            _feedHistoryRepository = feedHistoryRepository;
            _feedBidAskHistoryRepository = feedBidAskHistoryRepository;
            _candlesHistoryRepository = candlesHistoryRepository;
        }

        public async Task<DateTime> GetStartDateAsync(string assetPair, PriceType priceType)
        {
            var processedCandle = await _processedCandlesRepository.GetProcessedCandleAsync(assetPair, priceType);

            if (processedCandle != null)
            {
                return processedCandle.Date;
            }

            var oldestFeedHistory = await _feedHistoryRepository.GetTopRecordAsync(assetPair);

            return oldestFeedHistory?.DateTime ?? DateTime.UtcNow;
        }

        public async Task<DateTime> GetEndDateAsync(string assetPair, PriceType priceType)
        {
            var date = await _candlesHistoryRepository.GetTopRecordDateTimeAsync(assetPair, TimeInterval.Sec, priceType);

            return date ?? DateTime.UtcNow;
        }

        public async Task<IFeedHistory> GetFeedHistoryItemAsync(string assetPair, PriceType priceType, string rowKey)
        {
            return await _feedHistoryRepository.GetCandle(assetPair, priceType, rowKey);
        }

        public Task GetCandlesByChunkAsync(string assetPair, PriceType priceType, DateTime startDate, DateTime endDate, Func<IEnumerable<IFeedHistory>, PriceType, Task> callback)
        {
            return _feedHistoryRepository.GetCandlesByChunkAsync(assetPair, priceType, startDate, endDate, callback);
        }

        public Task GetHistoryBidAskByChunkAsync(string assetPair, DateTime startDate, DateTime endDate, Func<IEnumerable<IFeedBidAskHistory>, Task> callback)
        {
            return _feedBidAskHistoryRepository.GetHistoryByChunkAsync(assetPair, startDate, endDate, callback);
        }

        public async Task AddHistoryBidAskByChunkAsync(string assetPair, DateTime date, List<ICandle> askCandles, List<ICandle> bidCandles)
        {
            await _feedBidAskHistoryRepository.AddHistoryItemAsync(assetPair, date, askCandles, bidCandles);
        }

        public List<ICandle> GenerateMissedCandles(IFeedHistory feedHistory, TimeInterval interval)
        {
            var candles = feedHistory.Candles.ToList();

            if (candles[0].Tick != 1)
                GenerateFirstCandle(candles);

            if (candles[candles.Count - 1].Tick != 59)
                GenerateLastCandle(candles);

            var result = GenerateCandles(candles.ToList());

            return result.Select(item =>
                item.ToCandle(feedHistory.AssetPair, feedHistory.PriceType, feedHistory.DateTime, interval))
                .ToList();
        }

        public async Task SetProcessedCandleAsync(string assetPair, PriceType priceType, DateTime date)
        {
            await _processedCandlesRepository.AddProcessedCandleAsync(assetPair, priceType, date);
        }

        private void GenerateFirstCandle(IList<FeedHistoryItem> candles)
        {
            int places = candles[0].Open.Places();

            var rnd = new Random();
            var delta = candles.Count == 1 ? rnd.RandomSign() * 4 / Math.Pow(10, places) : candles[0].Open - candles[1].Open;
            var value = Math.Round(candles[0].Open + delta, places);
            candles.Insert(0, new FeedHistoryItem(value, value, value, value, 1));
        }

        private void GenerateLastCandle(IList<FeedHistoryItem> candles)
        {
            int places = candles[candles.Count - 1].Open.Places();

            var delta = candles[candles.Count - 1].Open - candles[candles.Count - 2].Open;
            var value = Math.Round(candles[candles.Count - 1].Open + delta, places);
            candles.Add(new FeedHistoryItem(value, value, value, value, 59));
        }

        private List<FeedHistoryItem> GenerateCandles(List<FeedHistoryItem> candles)
        {
            var rnd = new Random();
            var result = new List<FeedHistoryItem>();

            for (var i = 0; i < candles.Count - 1; i++)
            {
                var firstTick = candles[i].Tick;
                var lastTick = candles[i + 1].Tick;

                for (var s = firstTick + 1; s < lastTick; s++)
                {
                    double min = Math.Min(candles[i + 1].Open, candles[i].Open);
                    double max = Math.Max(candles[i + 1].Open, candles[i].Open);
                    var value = rnd.NextDouble(min, max);
                    var newCandle = new FeedHistoryItem(value, value, value, value, s);
                    result.Add(newCandle);
                }
            }

            candles.AddRange(result);
            return candles.OrderBy(item => item.Tick).ToList();
        }
    }
}
