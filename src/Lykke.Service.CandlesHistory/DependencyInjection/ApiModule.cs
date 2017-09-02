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
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services;
using Lykke.Service.CandlesHistory.Services.Assets;
using Lykke.Service.CandlesHistory.Services.Candles;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.CandlesHistory.DependencyInjection
{
    public class ApiModule : Module
    {
        private readonly AppSettings _settings;
        private readonly IServiceCollection _services;
        private readonly ILog _log;

        public ApiModule(AppSettings settings, ILog log)
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

            builder.RegisterType<Clock>().As<IClock>();

            RegisterAssets(builder);
            RegisterCandles(builder);

            builder.Populate(_services);
        }

        private void RegisterAssets(ContainerBuilder builder)
        {
            _services.UseAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.Assets.ServiceUrl),
                _settings.CandlesHistory.AssetsCache.ExpirationPeriod));

            builder.RegisterType<AssetPairsManager>()
                .As<IAssetPairsManager>();
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

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .SingleInstance();

            builder.RegisterType<CandlesSubscriber>()
                .As<ICandlesSubscriber>()
                .WithParameter(TypedParameter.From(_settings.CandlesHistory.CandlesSubscription))
                .SingleInstance();

            builder.RegisterType<CandlesManager>()
                .As<ICandlesManager>()
                .SingleInstance();

            builder.RegisterType<CandlesCacheService>()
                .As<ICandlesCacheService>()
                .As<IHaveState<IImmutableDictionary<string, IImmutableList<ICandle>>>>()
                .WithParameter(new TypedParameter(typeof(int), _settings.CandlesHistory.HistoryTicksCacheSize))
                .SingleInstance();

            builder.RegisterType<CandlesPersistenceManager>()
                .As<ICandlesPersistenceManager>()
                
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.CandlesHistory.Persistence));

            builder.RegisterType<CandlesPersistenceQueue>()
                .As<ICandlesPersistenceQueue>()
                .As<IHaveState<IImmutableList<ICandle>>>()
                .WithParameter(TypedParameter.From(_settings.CandlesHistory.Persistence))
                .SingleInstance();

            builder.RegisterType<QueueMonitor>()
                .As<IStartable>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.CandlesHistory.QueueMonitor))
                .AutoActivate();

            builder.RegisterType<FailedToPersistCandlesPublisher>()
                .As<IFailedToPersistCandlesPublisher>()
                .As<IStartable>()
                .WithParameter(TypedParameter.From(_settings.CandlesHistory.FailedToPersistPublication))
                .SingleInstance();

            builder.RegisterType<CandlesCacheInitalizationService>()
                .WithParameter(new TypedParameter(typeof(int), _settings.CandlesHistory.HistoryTicksCacheSize))
                .As<ICandlesCacheInitalizationService>();

            builder.RegisterType<CandlesCacheSnapshotRepository>()
                .As<ISnapshotRepository<IImmutableDictionary<string, IImmutableList<ICandle>>>>()
                .WithParameter(TypedParameter.From<IBlobStorage>(
                    new AzureBlobStorage(_settings.CandlesHistory.Db.SnapshotsConnectionString)));

            builder.RegisterType<SnapshotSerializer<IImmutableDictionary<string, IImmutableList<ICandle>>>>()
                .As<ISnapshotSerializer>()
                .AsSelf();

            builder.RegisterType<CandlesPersistenceQueueSnapshotRepository>()
                .As<ISnapshotRepository<IImmutableList<ICandle>>>()
                .WithParameter(TypedParameter.From<IBlobStorage>(
                    new AzureBlobStorage(_settings.CandlesHistory.Db.SnapshotsConnectionString)));

            builder.RegisterType<SnapshotSerializer<IImmutableList<ICandle>>>()
                .As<ISnapshotSerializer>()
                .AsSelf()
                .PreserveExistingDefaults();
        }
    }
}