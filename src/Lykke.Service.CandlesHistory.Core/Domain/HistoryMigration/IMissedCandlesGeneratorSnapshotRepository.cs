using System.Collections.Immutable;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration
{
    public interface IMissedCandlesGeneratorSnapshotRepository : ISnapshotRepository<IImmutableDictionary<string, ICandle>>
    {
    }
}