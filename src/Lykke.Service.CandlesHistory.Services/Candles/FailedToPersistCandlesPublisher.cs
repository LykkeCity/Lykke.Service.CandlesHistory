using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Settings;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class FailedToPersistCandlesPublisher : 
        IFailedToPersistCandlesPublisher,
        IDisposable
    {
        private readonly ILog _log;
        private readonly RabbitSettings _settings;
        private RabbitMqPublisher<FailedCandlesEnvelope> _publisher;

        public FailedToPersistCandlesPublisher(ILog log, RabbitSettings settings)
        {
            _log = log;
            _settings = settings;
        }

        public void Start()
        {
            var settings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = _settings.ConnectionString,
                ExchangeName = _settings.ExchangeName,
                RoutingKey = "",
                IsDurable = true
            };

            _publisher = new RabbitMqPublisher<FailedCandlesEnvelope>(settings)
                .SetSerializer(new JsonMessageSerializer<FailedCandlesEnvelope>())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                .SetLogger(_log)
                .Start();
        }

        public Task ProduceAsync(FailedCandlesEnvelope failedCandlesEnvelope)
        {
            return _publisher.ProduceAsync(failedCandlesEnvelope);
        }

        public void Dispose()
        {
            _publisher?.Dispose();
        }
    }
}