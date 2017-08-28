using System;
using System.Threading.Tasks;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesProducer : 
        ICandlesProducer,
        IDisposable
    {
       public void Start()
        {
            throw new NotImplementedException();
        }

        public Task ProduceAsync(IFeedCandle failedCandlesEnvelope)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}