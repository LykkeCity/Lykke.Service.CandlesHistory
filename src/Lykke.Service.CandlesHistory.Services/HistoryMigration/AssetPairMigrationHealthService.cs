using System;
using System.Collections.Generic;
using Common.Log;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    public class AssetPairMigrationHealthService
    {
        public class ProgressHistoryItem
        {
            public string Progress { get; }
            public DateTime Moment { get; }

            public ProgressHistoryItem(string progress)
            {
                Progress = progress;
                Moment = DateTime.UtcNow;
            }
        }

        public IReadOnlyList<ProgressHistoryItem> OverallProgressHistory => _overallProgressHistory;
        public DateTime? AskStartDate { get; private set; }
        public DateTime? BidStartDate { get; private set; }
        public DateTime AskEndDate { get; private set; }
        public DateTime BidEndDate { get; private set; }
        public DateTime CurrentAskDate { get; private set; }
        public DateTime CurrentBidDate { get; private set; }
        public DateTime CurrentMidDate { get; private set; }
        
        private readonly List<ProgressHistoryItem> _overallProgressHistory;
        private readonly ILog _log;
        private readonly string _assetPair;

        public AssetPairMigrationHealthService(ILog log, string assetPair)
        {
            _log = log;
            _assetPair = assetPair;
            _overallProgressHistory = new List<ProgressHistoryItem>();
        }

        public void UpdateOverallProgress(string progress)
        {
            _log.WriteInfoAsync(nameof(AssetPairMigrationHealthService), nameof(UpdateOverallProgress), _assetPair, progress);

            _overallProgressHistory.Add(new ProgressHistoryItem(progress));
        }

        public void UpdateStartDates(DateTime? askStartDate, DateTime? bidStartDate)
        {
            _log.WriteInfoAsync(nameof(AssetPairMigrationHealthService), nameof(UpdateStartDates), _assetPair, $"Start dates - ask: {askStartDate:O}, bid: {bidStartDate:O}");

            AskStartDate = askStartDate;
            BidStartDate = bidStartDate;
        }
        
        public void UpdateEndDates(DateTime askEndDate, DateTime bidEndDate)
        {
            _log.WriteInfoAsync(nameof(AssetPairMigrationHealthService), nameof(UpdateEndDates), _assetPair, $"End dates - ask: {askEndDate:O}, bid: {bidEndDate:O}");
            
            AskEndDate = askEndDate;
            BidEndDate = bidEndDate;
        }
        
        public void UpdateCurrentHistoryDate(DateTime date, PriceType priceType)
        {
            switch (priceType)
            {
                case PriceType.Bid:
                    CurrentBidDate = date;
                    break;

                case PriceType.Ask:
                    CurrentAskDate = date;
                    break;

                case PriceType.Mid:
                    CurrentMidDate = date;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(priceType), priceType, "Invalid price type");
            }
        }
    }
}
