using System;
using System.Collections.Immutable;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage;
using AzureStorage.Blob;
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
            var healthService = new HealthService();

            builder.RegisterInstance(healthService)
                .As<IHealthService>();

            builder.RegisterType<CandlesHistoryRepository>()
                .As<ICandlesHistoryRepository>()
                .WithParameter(
                    new TypedParameter(typeof(IImmutableDictionary<string, string>), 
                    _settings.CandleHistoryAssetConnections.ToImmutableDictionary()))
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder
                .RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .SingleInstance();

            builder.RegisterType<CandlesBroker>()
                .As<ICandlesBroker>()
                .SingleInstance();

            builder.RegisterType<MidPriceQuoteGenerator>()
                .As<IMidPriceQuoteGenerator>()
                .SingleInstance();

            builder.RegisterType<CandlesManager>()
                .As<ICandlesManager>()
                .SingleInstance();

            builder.RegisterType<CandlesCacheService>()
                .As<ICandlesCacheService>()
                .WithParameter(new TypedParameter(typeof(int), _settings.CandlesHistory.HistoryTicksCacheSize))
                .SingleInstance();

            builder.RegisterType<CandlesGenerator>()
                .As<ICandlesGenerator>();

            builder.RegisterType<CandlesPersistenceManager>()
                .As<ICandlesPersistenceManager>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.CandlesHistory.Persistence));

            builder.RegisterType<CandlesPersistenceQueue>()
                .As<ICandlesPersistenceQueue>()
                .WithParameter(TypedParameter.From(_settings.CandlesHistory.Persistence))
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

            builder.RegisterType<CandlesCacheDeserializationService>()
                .As<ICandlesCacheDeserializationService>();

            builder.RegisterType<CandlesCacheInitalizationService>()
                .WithParameter(new TypedParameter(typeof(int), _settings.CandlesHistory.HistoryTicksCacheSize))
                .As<ICandlesCacheInitalizationService>();

            builder.RegisterType<CandlesPersistenceQueueDeserializationService>()
                .As<ICandlesPersistenceQueueDeserializationService>();

            builder.RegisterType<CandlesCacheSerializationService>()
                .As<ICandlesCacheSerializationService>();

            builder.RegisterType<CandlesPersistenceQueueSerializationService>()
                .As<ICandlesPersistenceQueueSerializationService>();

            builder.RegisterType<CandlesCacheSnapshotRepository>()
                .As<ICandlesCacheSnapshotRepository>()
                .WithParameter(TypedParameter.From<IBlobStorage>(
                    new AzureBlobStorage(_settings.CandlesHistory.Db.SnapshotsConnectionString)));

            builder.RegisterType<CandlesPersistenceQueueSnapshotRepository>()
                .As<ICandlesPersistenceQueueSnapshotRepository>()
                .WithParameter(TypedParameter.From<IBlobStorage>(
                    new AzureBlobStorage(_settings.CandlesHistory.Db.SnapshotsConnectionString)));
        }
    }
}