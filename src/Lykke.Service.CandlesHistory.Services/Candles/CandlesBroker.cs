using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices.Model;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesBroker : ICandlesBroker
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
            var settings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = _settings.QuoteFeedRabbit.ConnectionString,
                QueueName = $"{_settings.QuoteFeedRabbit.ExchangeName}.candleshistory",
                ExchangeName = _settings.QuoteFeedRabbit.ExchangeName,
                DeadLetterExchangeName = _settings.QuoteFeedRabbit.DeadLetterExchangeName,
                RoutingKey = "",
                IsDurable = true
            };

            try
            {
                _subscriber = new RabbitMqSubscriber<IQuote>(settings, 
                    new ResilientErrorHandlingStrategy(_log, settings, 
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_log, settings)))
                    .SetMessageDeserializer(new JsonMessageDeserializer<Quote>())
                    .SetMessageReadStrategy(new MessageReadQueueStrategy())
                    .Subscribe(ProcessQuoteAsync)
                    .CreateDefaultBinding()
                    .SetLogger(_log)
                    .Start();
            }
            catch (Exception ex)
            {
                _log.WriteErrorAsync(nameof(CandlesBroker), nameof(Start), null, ex).Wait();
                throw;
            }
        }

        public void Stop()
        {
            _subscriber.Stop();
        }

        private async Task ProcessQuoteAsync(IQuote quote)
        {
            try
            {
                var validationErrors = ValidateQuote(quote);
                if (validationErrors.Any())
                {
                    var message = string.Join("\r\n", validationErrors);
                    await _log.WriteWarningAsync(nameof(CandlesBroker), nameof(ProcessQuoteAsync), quote.ToJson(), message);

                    return;
                }

                await _candlesManager.ProcessQuoteAsync(quote);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(CandlesBroker), nameof(ProcessQuoteAsync), null, ex);
            }
        }

        private static IReadOnlyCollection<string> ValidateQuote(IQuote quote)
        {
            var errors = new List<string>();

            if (quote == null)
            {
                errors.Add("Argument 'Order' is null.");
            }
            else
            {
                if (string.IsNullOrEmpty(quote.AssetPair))
                {
                    errors.Add(string.Format("Invalid 'AssetPair': '{0}'", quote.AssetPair ?? ""));
                }
                if (quote.Timestamp == DateTime.MinValue || quote.Timestamp == DateTime.MaxValue)
                {
                    errors.Add(string.Format("Invalid 'Timestamp' range: '{0}'", quote.Timestamp));
                }
                if (quote.Timestamp.Kind != DateTimeKind.Utc)
                {
                    errors.Add(string.Format("Invalid 'Timestamp' Kind (UTC is required): '{0}'", quote.Timestamp));
                }
            }

            return errors;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}