using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    internal static class CandleExtensions
    {
        public static CandleHistoryItem ToItem(this ICandle candle, int tick)
        {
            return new CandleHistoryItem
            {
                Open = candle.Open,
                Close = candle.Close,
                High = candle.High,
                Low = candle.Low,
                Tick = tick
            };
        }
    }
}
