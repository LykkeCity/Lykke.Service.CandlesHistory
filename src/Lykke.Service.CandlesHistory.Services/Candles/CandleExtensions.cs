using System.Collections.Generic;
using System.Linq;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public static class CandleExtensions
    {
        /// <summary>
        /// Merges candles into bigger intervals (e.g. Minute -> Min15).
        /// </summary>
        /// <param name="candles">Candles to merge</param>
        /// <param name="newInterval">New interval</param>
        public static IEnumerable<ICandle> MergeIntoBiggerIntervals(this IEnumerable<ICandle> candles, TimeInterval newInterval)
        {
            return candles
                .GroupBy(c => c.Timestamp.RoundTo(newInterval))
                .Select(g => g.MergeAll(g.Key));
        }
    }
}