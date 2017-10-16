using System;
using System.Linq;
using Common;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandleHistory.Repositories
{
    internal static class CandleExtensions
    {
        public static CandleHistoryItem ToItem(this ICandle candle, TimeInterval interval)
        {
            return new CandleHistoryItem
            {
                Open = candle.Open,
                Close = candle.Close,
                High = candle.High,
                Low = candle.Low,
                Tick = candle.Timestamp.GetIntervalTick(interval)
            };
        }
    }
}
