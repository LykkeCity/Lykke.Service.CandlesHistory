using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Settings;
using Newtonsoft.Json;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesSubscriber : ICandlesSubscriber
    {
        private class CandleMessage : ICandle
        {
            [JsonProperty("a")]
            public string AssetPairId { get; set; }

            [JsonProperty("p")]
            public PriceType PriceType { get; set; }

            [JsonProperty("i")]
            public TimeInterval TimeInterval { get; set; }

            [JsonProperty("t")]
            public DateTime Timestamp { get; set; }

            [JsonProperty("o")]
            public double Open { get; set; }

            [JsonProperty("c")]
            public double Close { get; set; }

            [JsonProperty("h")]
            public double High { get; set; }

            [JsonProperty("l")]
            public double Low { get; set; }
        }

        private readonly ILog _log;
        private readonly ICandlesManager _candlesManager;
        private readonly string _rabbitConnectionString;

        private RabbitMqSubscriber<CandleMessage> _subscriber;

        public CandlesSubscriber(ILog log, ICandlesManager candlesManager, string rabbitConnectionString)
        {
            _log = log;
            _candlesManager = candlesManager;
            _rabbitConnectionString = rabbitConnectionString;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .CreateForSubscriber(_rabbitConnectionString, "candles", "candleshistory")
                .MakeDurable()
                .DelayTheRecconectionForA(delay: TimeSpan.FromSeconds(20));

            try
            {
                _subscriber = new RabbitMqSubscriber<CandleMessage>(settings,
                        new ResilientErrorHandlingStrategy(_log, settings,
                            retryTimeout: TimeSpan.FromSeconds(10),
                            next: new DeadQueueErrorHandlingStrategy(_log, settings)))
                    .SetMessageDeserializer(new JsonMessageDeserializer<CandleMessage>())
                    .SetMessageReadStrategy(new MessageReadQueueStrategy())
                    .Subscribe(ProcessCandleAsync)
                    .CreateDefaultBinding()
                    .SetLogger(_log)
                    .Start();
            }
            catch (Exception ex)
            {
                _log.WriteErrorAsync(nameof(CandlesSubscriber), nameof(Start), null, ex).Wait();
                throw;
            }
        }

        public void Stop()
        {
            _subscriber?.Stop();
        }

        private async Task ProcessCandleAsync(CandleMessage candle)
        {
            try
            {
                var validationErrors = ValidateQuote(candle);
                if (validationErrors.Any())
                {
                    var message = string.Join("\r\n", validationErrors);
                    await _log.WriteWarningAsync(nameof(CandlesSubscriber), nameof(ProcessCandleAsync), candle.ToJson(), message);

                    return;
                }

                await _candlesManager.ProcessCandleAsync(candle);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(CandlesSubscriber), nameof(ProcessCandleAsync), null, ex);
            }
        }

        private static IReadOnlyCollection<string> ValidateQuote(CandleMessage candle)
        {
            var errors = new List<string>();

            if (candle == null)
            {
                errors.Add("Argument 'Order' is null.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(candle.AssetPairId))
                {
                    errors.Add("Empty 'AssetPair'");
                }
                if (candle.Timestamp.Kind != DateTimeKind.Utc)
                {
                    errors.Add($"Invalid 'Timestamp' Kind (UTC is required): '{candle.Timestamp.Kind}'");
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