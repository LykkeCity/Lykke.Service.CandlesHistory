using System.Linq;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;

namespace Lykke.Service.CandlesHistory.Core.Extensions
{
    public static class FeedHistoryItemExtensions
    {
        public static string ToFeedHistoryItem(this FeedHistoryItem candle, TimeInterval interval)
        {
            return $"O={candle.Open};C={candle.Close};H={candle.High};L={candle.Low};T={candle.Tick};";
        }

        public static string ToFeedHistoryData(this FeedHistoryItem[] candles, TimeInterval interval)
        {
            return string.Join('|', candles.Select(item => item.ToFeedHistoryItem(interval)));
        }
    }
}
