using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration.HistoryProviders.MeFeedHistory;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration.HistoryProviders.MeFeedHistory
{
    [UsedImplicitly]
    public class EmptyMissedCandlesGenerator : IMissedCandlesGenerator
    {
        public IReadOnlyList<ICandle> FillGapUpTo(IAssetPair assetPair, IFeedHistory feedHistory)
        {
            return feedHistory.Candles
                .Select(item => item.ToCandle(feedHistory.AssetPair, feedHistory.PriceType, feedHistory.DateTime))
                .ToList();
        }

        public IReadOnlyList<ICandle> FillGapUpTo(IAssetPair assetPair, CandlePriceType priceType, DateTime dateTime, ICandle endCandle)
        {
            return Array.Empty<ICandle>();
        }

        public void RemoveAssetPair(string assetPair)
        {
        }
    }
}
