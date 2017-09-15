using System;
using System.Collections.Generic;
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
using Lykke.Service.CandlesHistory.Services.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.CandlesHistory.DependencyInjection
{
    public class ApiModule : Module
    {        
        private readonly IServiceCollection _services;
        private readonly CandlesHistorySettings _candlesHistorySettings;
        private readonly Dictionary<string, string> _candleHistoryAssetConnections;
        private readonly AssetsSettings _assetsSettings;
        private readonly ILog _log;

        public ApiModule(CandlesHistorySettings candlesHistorySettings, Dictionary<string, string> candleHistoryAssetConnections, AssetsSettings assetsSettings, ILog log)
        {            
            _services = new ServiceCollection();
            _candlesHistorySettings = candlesHistorySettings;
            _candleHistoryAssetConnections = candleHistoryAssetConnections;
            _assetsSettings = assetsSettings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log).SingleInstance();
                        
            builder.RegisterInstance(_candlesHistorySettings).SingleInstance();

            builder.RegisterType<Clock>().As<IClock>();

            RegisterAssets(builder);
            RegisterCandles(builder);

            builder.Populate(_services);
        }

        private void RegisterAssets(ContainerBuilder builder)
        {
            _services.UseAssetsClient(AssetServiceSettings.Create(
                new Uri(_assetsSettings.ServiceUrl),
                _candlesHistorySettings.AssetsCache.ExpirationPeriod));

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
                        _candleHistoryAssetConnections.ToImmutableDictionary()))
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .SingleInstance();

            builder.RegisterType<CandlesSubscriber>()
                .As<ICandlesSubscriber>()
                .WithParameter(TypedParameter.From(_candlesHistorySettings.CandlesSubscription))
                .SingleInstance();

            builder.RegisterType<CandlesManager>()
                .As<ICandlesManager>()
                .SingleInstance();

            builder.RegisterType<CandlesCacheService>()
                .As<ICandlesCacheService>()
                .As<IHaveState<IImmutableDictionary<string, IImmutableList<ICandle>>>>()
                .WithParameter(new TypedParameter(typeof(int), _candlesHistorySettings.HistoryTicksCacheSize))
                .SingleInstance();

            builder.RegisterType<CandlesPersistenceManager>()
                .As<ICandlesPersistenceManager>()                
                .SingleInstance()
                .WithParameter(TypedParameter.From(_candlesHistorySettings.Persistence));

            builder.RegisterType<CandlesPersistenceQueue>()
                .As<ICandlesPersistenceQueue>()
                .As<IHaveState<IImmutableList<ICandle>>>()
                .WithParameter(TypedParameter.From(_candlesHistorySettings.Persistence))
                .SingleInstance();

            builder.RegisterType<QueueMonitor>()
                .As<IStartable>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_candlesHistorySettings.QueueMonitor))
                .AutoActivate();

            builder.RegisterType<FailedToPersistCandlesPublisher>()
                .As<IFailedToPersistCandlesPublisher>()
                .As<IStartable>()
                .WithParameter(TypedParameter.From(_candlesHistorySettings.FailedToPersistPublication))
                .SingleInstance();

            builder.RegisterType<CandlesCacheInitalizationService>()
                .WithParameter(new TypedParameter(typeof(int), _candlesHistorySettings.HistoryTicksCacheSize))
                .As<ICandlesCacheInitalizationService>();

            builder.RegisterType<CandlesCacheSnapshotRepository>()
                .As<ISnapshotRepository<IImmutableDictionary<string, IImmutableList<ICandle>>>>()
                .WithParameter(TypedParameter.From<IBlobStorage>(
                    new AzureBlobStorage(_candlesHistorySettings.Db.SnapshotsConnectionString)));

            builder.RegisterType<SnapshotSerializer<IImmutableDictionary<string, IImmutableList<ICandle>>>>()
                .As<ISnapshotSerializer>()
                .AsSelf();

            builder.RegisterType<CandlesPersistenceQueueSnapshotRepository>()
                .As<ISnapshotRepository<IImmutableList<ICandle>>>()
                .WithParameter(TypedParameter.From<IBlobStorage>(
                    new AzureBlobStorage(_candlesHistorySettings.Db.SnapshotsConnectionString)));

            builder.RegisterType<SnapshotSerializer<IImmutableList<ICandle>>>()
                .As<ISnapshotSerializer>()
                .AsSelf()
                .PreserveExistingDefaults();
        }
    }
}