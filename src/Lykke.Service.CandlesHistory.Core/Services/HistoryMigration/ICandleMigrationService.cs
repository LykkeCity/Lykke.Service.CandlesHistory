using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.HistoryMigration
{
    public interface ICandlesHistoryMigrationService
    {
        Task<ICandle> GetFirstCandleOfHistoryAsync(string assetPair, CandlePriceType priceType);
    }
}
