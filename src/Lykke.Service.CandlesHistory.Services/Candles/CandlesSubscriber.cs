using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Settings;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class CandlesSubscriber : ICandlesSubscriber
    {
        private readonly ILog _log;
        private readonly ICandlesManager _candlesManager;
        private readonly RabbitEndpointSettings _settings;

        private RabbitMqSubscriber<CandleMessage> _subscriber;

        public CandlesSubscriber(ILog log, ICandlesManager candlesManager, RabbitEndpointSettings settings)
        {
            _log = log;
            _candlesManager = candlesManager;
            _settings = settings;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .CreateForSubscriber(_settings.ConnectionString, _settings.Namespace, "candles", _settings.Namespace, "candleshistory")
                .MakeDurable();

            try
            {
                _subscriber = new RabbitMqSubscriber<CandleMessage>(settings,
                        new ResilientErrorHandlingStrategy(_log, settings,
                            retryTimeout: TimeSpan.FromSeconds(10),
                            retryNum: 10,
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

                _candlesManager.ProcessCandle(new Candle(
                    assetPair: candle.AssetPairId,
                    priceType: candle.PriceType,
                    timeInterval: candle.TimeInterval,
                    timestamp: candle.Timestamp,
                    open: candle.Open,
                    close: candle.Close,
                    low: candle.Low,
                    high: candle.High,
                    tradingVolume: candle.TradingVolume));
            }
            catch (Exception)
            {
                await _log.WriteWarningAsync(nameof(CandlesSubscriber), nameof(ProcessCandleAsync), candle.ToJson(), "Failed to process candle");
                throw;
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
