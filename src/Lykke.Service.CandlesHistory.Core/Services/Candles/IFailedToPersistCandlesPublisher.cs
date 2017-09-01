using System.Threading.Tasks;
using Autofac;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface IFailedToPersistCandlesPublisher : IStartable
    {
        Task ProduceAsync(FailedCandlesEnvelope failedCandlesEnvelope);
    }
}