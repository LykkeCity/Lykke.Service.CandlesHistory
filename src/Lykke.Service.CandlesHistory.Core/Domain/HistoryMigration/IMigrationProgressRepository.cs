using System;
using System.Threading.Tasks;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration
{
    public interface IMigrationProgressRepository
    {
        Task<DateTime?> GetProcessedDateAsync(string assetPair, PriceType priceType);
        Task SetProcessedDateAsync(string assetPair, PriceType priceType, DateTime date);
        Task RemoveProcessedDateAsync(string assetPair);
    }
}
