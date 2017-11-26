using System;
using System.Collections.Generic;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration.HistoryProviders.MeFeedHistory;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration.HistoryProviders.MeFeedHistory
{
    public interface IMissedCandlesGenerator
    {
        IReadOnlyList<ICandle> FillGapUpTo(IAssetPair assetPair, IFeedHistory feedHistory);
        IReadOnlyList<ICandle> FillGapUpTo(IAssetPair assetPair, CandlePriceType priceType, DateTime dateTime, ICandle endCandle);
        void RemoveAssetPair(string assetPair);
    }
}
