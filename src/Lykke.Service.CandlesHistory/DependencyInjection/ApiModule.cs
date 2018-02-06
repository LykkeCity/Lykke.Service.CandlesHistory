using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Blob;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandleHistory.Repositories.Candles;
using Lykke.Service.CandleHistory.Repositories.HistoryMigration.HistoryProviders.MeFeedHistory;
using Lykke.Service.CandleHistory.Repositories.Snapshots;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration.HistoryProviders.MeFeedHistory;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration.HistoryProviders;
using Lykke.Service.CandlesHistory.Services;
using Lykke.Service.CandlesHistory.Services.Assets;
using Lykke.Service.CandlesHistory.Services.Candles;
using Lykke.Service.CandlesHistory.Services.HistoryMigration;
using Lykke.Service.CandlesHistory.Services.HistoryMigration.HistoryProviders;
using Lykke.Service.CandlesHistory.Services.HistoryMigration.HistoryProviders.MeFeedHistory;
using Lykke.Service.CandlesHistory.Services.Settings;
using Lykke.Service.CandlesHistory.Validation;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.CandlesHistory.DependencyInjection
{
    public class ApiModule : Module
    {
        private readonly IServiceCollection _services;
        private readonly CandlesHistorySettings _settings;
        private readonly AssetsSettings _assetSettings;
        private readonly IReloadingManager<Dictionary<string, string>> _candleHistoryAssetConnections;
        private readonly IReloadingManager<DbSettings> _dbSettings;
        private readonly ILog _log;

        public ApiModule(
            CandlesHistorySettings settings,
            AssetsSettings assetSettings,  
            IReloadingManager<Dictionary<string, string>> candleHistoryAssetConnections,  
            IReloadingManager<DbSettings> dbSettings,
            ILog log)
        {
            _services = new ServiceCollection();
            _settings = settings;
            _assetSettings = assetSettings;
            _candleHistoryAssetConnections = candleHistoryAssetConnections;
            _dbSettings = dbSettings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();
                        
            builder.RegisterType<Clock>().As<IClock>();

            // For CandlesHistoryController
            builder.RegisterInstance(_candleHistoryAssetConnections.CurrentValue)
                .AsSelf();

            RegisterAssets(builder);
            RegisterCandles(builder);

            builder.Populate(_services);
        }

        private void RegisterAssets(ContainerBuilder builder)
        {
            _services.UseAssetsClient(AssetServiceSettings.Create(
                new Uri(_assetSettings.ServiceUrl),
                _settings.AssetsCache.ExpirationPeriod));

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
                .WithParameter(TypedParameter.From(_candleHistoryAssetConnections))
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .SingleInstance();

            builder.RegisterType<SnapshotSerializer>()
                .As<ISnapshotSerializer>();

            builder.RegisterType<CandlesSubscriber>()
                .As<ICandlesSubscriber>()
                .WithParameter(TypedParameter.From(_settings.Rabbit.CandlesSubscription))
                .SingleInstance();

            builder.RegisterType<CandlesManager>()
                .As<ICandlesManager>()
                .WithParameter(TypedParameter.From(_settings.ErrorManagement))
                .SingleInstance();

            builder.RegisterType<CandlesCacheService>()
                .As<ICandlesCacheService>()
                .WithParameter(TypedParameter.From(_settings.HistoryTicksCacheSize))
                .SingleInstance();

            builder.RegisterType<CandlesPersistenceManager>()
                .As<ICandlesPersistenceManager>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.Persistence));

            builder.RegisterType<CandlesPersistenceQueue>()
                .As<ICandlesPersistenceQueue>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.Persistence));

            builder.RegisterType<QueueMonitor>()
                .As<IStartable>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.QueueMonitor))
                .AutoActivate();

            builder.RegisterType<CandlesCacheInitalizationService>()
                .WithParameter(TypedParameter.From(_settings.HistoryTicksCacheSize))
                .As<ICandlesCacheInitalizationService>();

            builder.RegisterType<CandlesCacheSnapshotRepository>()
                .As<ICandlesCacheSnapshotRepository>()
                .WithParameter(TypedParameter.From(AzureBlobStorage.Create(_dbSettings.ConnectionString(x => x.SnapshotsConnectionString), TimeSpan.FromMinutes(10))));

            builder.RegisterType<CandlesPersistenceQueueSnapshotRepository>()
                .As<ICandlesPersistenceQueueSnapshotRepository>()
                .WithParameter(TypedParameter.From(AzureBlobStorage.Create(_dbSettings.ConnectionString(x => x.SnapshotsConnectionString), TimeSpan.FromMinutes(10))));

            builder.RegisterType<CandlesHistorySizeValidator>()
                .AsSelf()
                .WithParameter(TypedParameter.From(_settings.MaxCandlesCountWhichCanBeRequested));

            RegisterCandlesMigration(builder);
        }

        private void RegisterCandlesMigration(ContainerBuilder builder)
        {
            if (!string.IsNullOrWhiteSpace(_dbSettings.CurrentValue.FeedHistoryConnectionString))
            {
                builder.RegisterType<FeedHistoryRepository>()
                    .As<IFeedHistoryRepository>()
                    .WithParameter(TypedParameter.From(AzureTableStorage<FeedHistoryEntity>.Create(
                        _dbSettings.ConnectionString(x => x.FeedHistoryConnectionString), 
                        "FeedHistory", 
                        _log, 
                        maxExecutionTimeout: TimeSpan.FromMinutes(5))))
                    .SingleInstance();
            }

            builder.RegisterType<CandlesMigrationManager>()
                .AsSelf()
                .WithParameter(TypedParameter.From(_settings.Migration))
                .SingleInstance();

            builder.RegisterType<CandlesesHistoryMigrationService>()
                .As<ICandlesHistoryMigrationService>()
                .SingleInstance();

            builder.RegisterType<MigrationCandlesGenerator>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<EmptyMissedCandlesGenerator>()
                .As<IMissedCandlesGenerator>()
                .SingleInstance();

            builder.RegisterType<HistoryProvidersManager>()
                .As<IHistoryProvidersManager>()
                .SingleInstance();
                
            RegisterHistoryProvider<MeFeedHistoryProvider>(builder);
        }

        private static void RegisterHistoryProvider<TProvider>(ContainerBuilder builder) 
            where TProvider : IHistoryProvider
        {
            builder.RegisterType<TProvider>()
                .Named<IHistoryProvider>(typeof(TProvider).Name);
        }
    }
}
