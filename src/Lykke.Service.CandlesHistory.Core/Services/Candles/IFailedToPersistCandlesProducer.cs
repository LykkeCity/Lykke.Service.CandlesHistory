using System.Threading.Tasks;
using Autofac;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface IFailedToPersistCandlesProducer : IStartable
    {
        Task ProduceAsync(FailedCandlesEnvelope failedCandlesEnvelope);
    }
}