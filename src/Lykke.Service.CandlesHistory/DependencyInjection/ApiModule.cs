using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Blob;
using Common.Log;
using Lykke.RabbitMq.Azure;
using Lykke.RabbitMqBroker.Publisher;
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
using Lykke.Service.CandlesHistory.Services.Settings;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.CandlesHistory.DependencyInjection
{
    public class ApiModule : Module
    {
        private readonly IServiceCollection _services;
        private readonly IReloadingManager<CandlesHistorySettings> _settings;
        private readonly IReloadingManager<AssetsSettings> _assetSettings;
        private readonly IReloadingManager<Dictionary<string, string>> _candleHistoryAssetConnections;
        private readonly ILog _log;

        public ApiModule(
            IReloadingManager<CandlesHistorySettings> settings, 
            IReloadingManager<AssetsSettings> assetSettings,  
            IReloadingManager<Dictionary<string, string>> candleHistoryAssetConnections,  
            ILog log)
        {
            _services = new ServiceCollection();
            _settings = settings;
            _assetSettings = assetSettings;
            _candleHistoryAssetConnections = candleHistoryAssetConnections;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();
                        
            builder.RegisterType<Clock>().As<IClock>();

            //TODO: registering for CandlesHistoryController, is it ok?
            builder.RegisterInstance(
                _candleHistoryAssetConnections.CurrentValue
            ).AsSelf();

            RegisterAssets(builder);
            RegisterCandles(builder);

            builder.Populate(_services);
        }

        private void RegisterAssets(ContainerBuilder builder)
        {
            _services.UseAssetsClient(AssetServiceSettings.Create(
                new Uri(_assetSettings.CurrentValue.ServiceUrl),
                _settings.CurrentValue.AssetsCache.ExpirationPeriod));

            builder.RegisterType<AssetPairsManager>()
                .As<IAssetPairsManager>();
        }

        private void RegisterCandles(ContainerBuilder builder)
        {
            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<CandlesHistoryRepository>()
                .As<ICandlesHistoryRepository>()
                .WithParameter(
                    new TypedParameter(typeof(IImmutableDictionary<string, string>),
                        _candleHistoryAssetConnections.CurrentValue.ToImmutableDictionary()))
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .SingleInstance();

            builder.RegisterType<CandlesSubscriber>()
                .As<ICandlesSubscriber>()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.Rabbit.CandlesSubscription))
                .SingleInstance();

            builder.RegisterType<CandlesManager>()
                .As<ICandlesManager>()
                .SingleInstance();

            builder.RegisterType<CandlesCacheService>()
                .As<ICandlesCacheService>()
                .As<IHaveState<IImmutableDictionary<string, IImmutableList<ICandle>>>>()
                .WithParameter(new TypedParameter(typeof(int), _settings.CurrentValue.HistoryTicksCacheSize))
                .SingleInstance();

            builder.RegisterType<CandlesPersistenceManager>()
                .As<ICandlesPersistenceManager>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.Persistence));

            builder.RegisterType<CandlesPersistenceQueue>()
                .As<ICandlesPersistenceQueue>()
                .As<IHaveState<IImmutableList<ICandle>>>()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.Persistence))
                .SingleInstance();

            builder.RegisterType<QueueMonitor>()
                .As<IStartable>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.QueueMonitor))
                .AutoActivate();

            builder.RegisterType<FailedToPersistCandlesPublisher>()
                .As<IFailedToPersistCandlesPublisher>()
                .As<IStartable>()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.Rabbit.FailedToPersistPublication))
                .WithParameter(TypedParameter.From<IPublishingQueueRepository<IFailedCandlesEnvelope>>(
                    new BlobPublishingQueueRepository<FailedCandlesEnvelope, IFailedCandlesEnvelope>(
                        AzureBlobStorage.Create(_settings.ConnectionString(x => x.Db.SnapshotsConnectionString)))))
                .SingleInstance();

            builder.RegisterType<CandlesCacheInitalizationService>()
                .WithParameter(new TypedParameter(typeof(int), _settings.CurrentValue.HistoryTicksCacheSize))
                .As<ICandlesCacheInitalizationService>();

            builder.RegisterType<CandlesCacheSnapshotRepository>()
                .As<ISnapshotRepository<IImmutableDictionary<string, IImmutableList<ICandle>>>>()
                .WithParameter(TypedParameter.From(AzureBlobStorage.Create(_settings.ConnectionString(x => x.Db.SnapshotsConnectionString))));

            builder.RegisterType<SnapshotSerializer<IImmutableDictionary<string, IImmutableList<ICandle>>>>()
                .As<ISnapshotSerializer>()
                .AsSelf();

            builder.RegisterType<CandlesPersistenceQueueSnapshotRepository>()
                .As<ISnapshotRepository<IImmutableList<ICandle>>>()
                .WithParameter(TypedParameter.From(AzureBlobStorage.Create(_settings.ConnectionString(x => x.Db.SnapshotsConnectionString))));

            builder.RegisterType<SnapshotSerializer<IImmutableList<ICandle>>>()
                .As<ISnapshotSerializer>()
                .AsSelf()
                .PreserveExistingDefaults();
        }
    }
}
