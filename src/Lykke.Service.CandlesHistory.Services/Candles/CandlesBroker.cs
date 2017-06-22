using System;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices.Model;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.RabbitMq;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesBroker : 
        IStartable,
        IDisposable
    {
        private readonly ILog _log;
        private readonly ICandlesManager _candlesManager;
        private readonly ApplicationSettings.CandlesHistorySettings _settings;
        private RabbitMqSubscriber<IQuote> _subscriber;

        public CandlesBroker(ILog log, ICandlesManager candlesManager, ApplicationSettings.CandlesHistorySettings settings)
        {
            _log = log;
            _candlesManager = candlesManager;
            _settings = settings;
        }

        public void Start()
        {
            try
            {
                _subscriber = new RabbitMqSubscriber<IQuote>(new RabbitMqSubscriberSettings
                    {
                        ConnectionString = _settings.QuoteFeedRabbit.ConnectionString,
                        QueueName = $"{_settings.QuoteFeedRabbit.ExchangeName}.candleshistory",
                        ExchangeName = _settings.QuoteFeedRabbit.ExchangeName
                    })
                    .SetMessageDeserializer(new JsonMessageDeserializer<Quote>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .Subscribe(ProcessQuoteAsync)
                    .SetLogger(_log)
                    .Start();
            }
            catch (Exception ex)
            {
                _log.WriteErrorAsync(Constants.ComponentName, null, null, ex).Wait();
                throw;
            }
        }

        private async Task ProcessQuoteAsync(IQuote quote)
        {
            try
            {
                await _candlesManager.ProcessQuoteAsync(quote);
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