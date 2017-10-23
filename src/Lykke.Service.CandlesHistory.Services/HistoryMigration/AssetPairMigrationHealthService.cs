using System;
using System.Collections.Generic;
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

        public AssetPairMigrationHealthService()
        {
            _overallProgressHistory = new List<ProgressHistoryItem>();
        }

        public void UpdateOverallProgress(string progress)
        {
            _overallProgressHistory.Add(new ProgressHistoryItem(progress));
        }

        public void UpdateStartDates(DateTime? askStartDate, DateTime? bidStartDate)
        {
            AskStartDate = askStartDate;
            BidStartDate = bidStartDate;
        }
        
        public void UpdateEndDates(DateTime askEndDate, DateTime bidEndDate)
        {
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
