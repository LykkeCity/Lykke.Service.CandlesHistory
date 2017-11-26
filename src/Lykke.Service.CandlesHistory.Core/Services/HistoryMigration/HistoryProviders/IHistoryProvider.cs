using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.HistoryMigration.HistoryProviders
{
    public interface IHistoryProvider
    {
        Task<DateTime?> GetStartDateAsync(string assetPair, CandlePriceType priceType);
        Task GetHistoryByChunksAsync(
            IAssetPair assetPair,
            CandlePriceType priceType,
            DateTime endDate,
            ICandle endCandle,
            Func<IReadOnlyList<ICandle>, Task> readChunkFunc,
            CancellationToken cancellationToken);
    }
}
