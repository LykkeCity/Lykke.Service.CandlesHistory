using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class MigrationCandlesGenerator : IHaveState<IImmutableDictionary<string, ICandle>>
    {
        private ConcurrentDictionary<string, MigrationCandle> _candles;

        public MigrationCandlesGenerator()
        {
            _candles = new ConcurrentDictionary<string, MigrationCandle>();
        }

        public MigrationCandleMergeResult Merge(ICandle candle, CandleTimeInterval timeInterval)
        {
            var key = GetKey(candle.AssetPairId, timeInterval, candle.PriceType);

            MigrationCandle oldCandle = null;
            var newCandle = _candles.AddOrUpdate(key,
                addValueFactory: k => MigrationCandle.Create(candle, timeInterval),
                updateValueFactory: (k, old) =>
                {
                    oldCandle = old;
                    return oldCandle.Merge(candle);
                });

            return new MigrationCandleMergeResult(newCandle, !newCandle.Equals(oldCandle));
        }

        private static string GetKey(string assetPair, CandleTimeInterval timeInterval, CandlePriceType type)
        {
            return $"{assetPair}-{type}-{timeInterval}";
        }

        public IImmutableDictionary<string, ICandle> GetState()
        {
            return _candles.ToArray().ToImmutableDictionary(i => i.Key, i => (ICandle)i.Value);
        }

        public void SetState(IImmutableDictionary<string, ICandle> state)
        {
            if (_candles.Count > 0)
            {
                throw new InvalidOperationException("Candles generator state already not empty");
            }

            _candles = new ConcurrentDictionary<string, MigrationCandle>(state.ToDictionary(
                i => i.Key,
                i => MigrationCandle.Create(i.Value)));
        }

        public string DescribeState(IImmutableDictionary<string, ICandle> state)
        {
            return $"Candles count: {state.Count}";
        }

        public void RemoveAssetPair(string assetPair)
        {
            foreach (var priceType in Constants.StoredPriceTypes)
            {
                foreach (var timeInterval in Constants.StoredIntervals)
                {
                    _candles.TryRemove(GetKey(assetPair, timeInterval, priceType), out var _);
                }
            }
        }
    }
}
