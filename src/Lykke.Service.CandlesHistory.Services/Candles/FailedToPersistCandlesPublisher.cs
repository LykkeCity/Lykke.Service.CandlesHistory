using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Settings;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class FailedToPersistCandlesPublisher : IFailedToPersistCandlesPublisher, IDisposable
    {
        private readonly ILog _log;
        private readonly IPublishingQueueRepository _publishingQueueRepository;
        private readonly RabbitEndpointSettings _settings;
        private RabbitMqPublisher<IFailedCandlesEnvelope> _publisher;

        public FailedToPersistCandlesPublisher(
            ILog log, 
            IPublishingQueueRepository publishingQueueRepository, 
            RabbitEndpointSettings settings)
        {
            _log = log;
            _publishingQueueRepository = publishingQueueRepository;
            _settings = settings;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .CreateForPublisher(_settings.ConnectionString, $"{_settings.Namespace}.candleshistory", "failedtopersist")
                .MakeDurable()
                .DelayTheRecconectionForA(delay: TimeSpan.FromSeconds(20));

            _publisher = new RabbitMqPublisher<IFailedCandlesEnvelope>(settings)
                .SetSerializer(new JsonMessageSerializer<IFailedCandlesEnvelope>())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                .SetQueueRepository(_publishingQueueRepository)
                .SetLogger(_log)
                .Start();
        }

        public Task ProduceAsync(IFailedCandlesEnvelope failedCandlesEnvelope)
        {
            return _publisher.ProduceAsync(FailedCandlesEnvelope.Create(failedCandlesEnvelope));
        }

        public void Dispose()
        {
            _publisher?.Dispose();
        }
    }
}
