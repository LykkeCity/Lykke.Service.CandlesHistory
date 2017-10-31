using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;

namespace Lykke.Service.CandlesHistory.Core.Services.HistoryMigration
{
    public interface ICandlesMigrationService
    {
        Task<DateTime?> GetStartDateAsync(string assetPair, PriceType priceType);
        Task<DateTime> GetEndDateAsync(string assetPair, PriceType priceType, DateTime now);

        Task GetFeedHistoryCandlesByChunkAsync(string assetPair, PriceType priceType, DateTime startDate, DateTime endDate,
            Func<IEnumerable<IFeedHistory>, PriceType, Task> callback);
    }
}
