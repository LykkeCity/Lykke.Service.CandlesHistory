using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.HistoryMigration
{
    public interface ICandlesHistoryMigrationService
    {
        Task<ICandle> GetFirstCandleOfHistoryAsync(string assetPair, PriceType priceType);
    }
}
