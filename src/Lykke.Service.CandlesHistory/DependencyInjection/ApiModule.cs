using System;
using System.Collections.Immutable;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using AzureStorage.Tables.Decorators;
using Common.Log;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandleHistory.Repositories;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services;
using Lykke.Service.CandlesHistory.Services.Assets;
using Lykke.Service.CandlesHistory.Services.Candles;
using Microsoft.Extensions.DependencyInjection;
using DateTimeProvider = Lykke.Service.CandlesHistory.Services.DateTimeProvider;
using IDateTimeProvider = Lykke.Service.CandlesHistory.Core.Services.IDateTimeProvider;

namespace Lykke.Service.CandlesHistory.DependencyInjection
{
    public class ApiModule : Module
    {
        private readonly ApplicationSettings _settings;
        private readonly IServiceCollection _services;
        private readonly ILog _log;

        public ApiModule(ApplicationSettings settings, ILog log)
        {
            _settings = settings;
            _services = new ServiceCollection();
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log).SingleInstance();

            builder.RegisterInstance(_settings).SingleInstance();
            builder.RegisterInstance(_settings.CandlesHistory).SingleInstance();

            builder.RegisterType<DateTimeProvider>().As<IDateTimeProvider>();

            RegisterAssets(builder);
            RegisterCandles(builder);

            builder.Populate(_services);
        }

        private void RegisterAssets(ContainerBuilder builder)
        {
            _services.UseAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.CandlesHistory.Dictionaries.AssetsServiceUrl),
                _settings.CandlesHistory.Dictionaries.CacheExpirationPeriod));

            builder.RegisterType<AssetPairsManager>()
                .As<IAssetPairsManager>()
                .WithParameter(new TypedParameter(typeof(TimeSpan), _settings.CandlesHistory.Dictionaries.CacheExpirationPeriod))
                .SingleInstance();
        }

        private void RegisterCandles(ContainerBuilder builder)
        {
            var candlesHistoryAssetConnections = _settings.CandleHistoryAssetConnections.ToImmutableDictionary();

            builder.RegisterInstance(new CandleHistoryRepository((assetPair, tableName) =>
                {
                    if (!candlesHistoryAssetConnections.TryGetValue(assetPair, out string assetConnectionString) ||
                        string.IsNullOrEmpty(assetConnectionString))
                    {
                        throw new ConfigurationException($"Connection string for asset pair '{assetPair}' is not specified.");
                    }

                    var storage = new AzureTableStorage<CandleTableEntity>(assetConnectionString, tableName, _log);

                    // Create and preload table info
                    storage.GetDataAsync(assetPair, "1900-01-01").Wait();

                    return new RetryOnFailureAzureTableStorageDecorator<CandleTableEntity>(storage, 5, 5, TimeSpan.FromSeconds(10));
                }))
                .As<ICandleHistoryRepository>();
           
            builder.RegisterType<CandlesBroker>()
                .As<ICandlesBroker>()
                .As<IStartable>()
                .SingleInstance()
                .AutoActivate();

            builder.RegisterType<CandlesManager>()
                .As<ICandlesManager>()
                .As<IStartable>()
                .WithParameter(new TypedParameter(typeof(int), _settings.CandlesHistory.HistoryTicksCacheSize))
                .WithParameter(new TypedParameter(typeof(IImmutableDictionary<string, string>), candlesHistoryAssetConnections))
                .SingleInstance();

            builder.RegisterType<CandlesCacheService>()
                .As<ICandlesCacheService>()
                .WithParameter(new TypedParameter(typeof(int), _settings.CandlesHistory.HistoryTicksCacheSize))
                .SingleInstance();

            builder.RegisterType<CandlesPersistenceManager>()
                .As<IStartable>()
                .SingleInstance()
                .AutoActivate();

            builder.RegisterType<CandlesPersistenceQueue>()
                .As<ICandlesPersistenceQueue>()
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<QueueMonitor>()
                .As<IStartable>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.CandlesHistory.QueueMonitor))
                .AutoActivate();

            builder.RegisterType<FailedToPersistCandlesProducer>()
                .As<IFailedToPersistCandlesProducer>()
                .As<IStartable>()
                .SingleInstance();

            builder
                .RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .SingleInstance();
        }
    }
}