using System;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices.Model;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Services.RabbitMq;

namespace Lykke.Service.CandlesHistory.Services
{
    public class CandlesBroker : 
        IStartable,
        IDisposable
    {
        private readonly ILog _log;
        private readonly ICandlesService _quotesService;
        private readonly ApplicationSettings.CandlesHistorySettings _settings;
        private RabbitMqSubscriber<IQuote> _subscriber;

        public CandlesBroker(ILog log, ICandlesService quotesService, ApplicationSettings.CandlesHistorySettings settings)
        {
            _log = log;
            _quotesService = quotesService;
            _settings = settings;
        }

        public void Start()
        {
            try
            {
                _subscriber = new RabbitMqSubscriber<IQuote>(new RabbitMqSubscriberSettings
                    {
                        ConnectionString = _settings.QuoteFeedRabbitSettings.ConnectionString,
                        QueueName = $"{_settings.QuoteFeedRabbitSettings.ExchangeName}.candleshistory",
                        ExchangeName = _settings.QuoteFeedRabbitSettings.ExchangeName
                    })
                    .SetMessageDeserializer(new JsonMessageDeserializer<Quote>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .Subscribe(ProcessQuote)
                    .SetLogger(_log)
                    .Start();
            }
            catch (Exception ex)
            {
                _log.WriteErrorAsync(Constants.ComponentName, null, null, ex).Wait();
                throw;
            }
        }

        private async Task ProcessQuote(IQuote quote)
        {
            try
            {
                _quotesService.AddQuote(quote);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(Constants.ComponentName, null, null, ex);
            }
        }

        public void Dispose()
        {
            _subscriber.Stop();
        }
    }
}