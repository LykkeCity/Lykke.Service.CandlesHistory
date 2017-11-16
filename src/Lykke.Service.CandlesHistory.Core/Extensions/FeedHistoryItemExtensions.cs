using System.Linq;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration.HistoryProviders.MeFeedHistory;

namespace Lykke.Service.CandlesHistory.Core.Extensions
{
    public static class FeedHistoryItemExtensions
    {
        private static string ToFeedHistoryItem(this FeedHistoryItem candle)
        {
            return $"O={candle.Open};C={candle.Close};H={candle.High};L={candle.Low};T={candle.Tick};";
        }

        public static string ToFeedHistoryData(this FeedHistoryItem[] candles)
        {
            return string.Join('|', candles.Select(item => item.ToFeedHistoryItem()));
        }
    }
}
