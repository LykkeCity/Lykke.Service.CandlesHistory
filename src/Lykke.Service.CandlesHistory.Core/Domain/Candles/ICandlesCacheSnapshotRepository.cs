using System.Collections.Immutable;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public interface ICandlesCacheSnapshotRepository : ISnapshotRepository<IImmutableDictionary<string, IImmutableList<ICandle>>>
    {
    }
}