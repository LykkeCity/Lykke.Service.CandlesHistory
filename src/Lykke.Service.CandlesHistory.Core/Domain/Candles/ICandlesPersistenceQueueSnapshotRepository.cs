using System.Collections.Immutable;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public interface ICandlesPersistenceQueueSnapshotRepository : ISnapshotRepository<IImmutableList<ICandle>>
    {
    }
}
