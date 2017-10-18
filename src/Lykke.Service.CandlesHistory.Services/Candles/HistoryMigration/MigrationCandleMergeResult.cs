using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles.HistoryMigration
{
    public class MigrationCandleMergeResult
    {
        public ICandle Candle { get; }
        public bool WasChanged { get; }

        public MigrationCandleMergeResult(ICandle candle, bool wasChanged)
        {
            Candle = candle;
            WasChanged = wasChanged;
        }
    }
}
