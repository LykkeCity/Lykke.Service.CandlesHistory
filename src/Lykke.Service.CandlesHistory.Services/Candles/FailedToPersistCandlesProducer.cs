using System.Threading.Tasks;
using Common.Log;
using Lykke.Domain.Prices.Contracts;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class FailedToPersistCandlesProducer : IFailedToPersistCandlesProducer
    {
        private readonly ILog _log;
        private readonly ApplicationSettings.CandlesHistorySettings _settings;
        private RabbitMqPublisher<FailedCandlesEnvelope> _publisher;

        public FailedToPersistCandlesProducer(ILog log, ApplicationSettings.CandlesHistorySettings settings)
        {
            _log = log;
            _settings = settings;
        }

        public void Start()
        {
            var settings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = _settings.FailedToPersistRabbit.ConnectionString,
                ExchangeName = _settings.FailedToPersistRabbit.ExchangeName,
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
    }
}