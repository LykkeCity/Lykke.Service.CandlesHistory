using System.Collections.Immutable;
using Autofac;
using Common;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesPersistenceQueue : IStartable, IStopable, IHaveState<IImmutableList<ICandle>>
    {
        void DispatchCandlesToPersist(int maxBatchSize);
        void EnqueueCandle(ICandle candle);
    }
}
