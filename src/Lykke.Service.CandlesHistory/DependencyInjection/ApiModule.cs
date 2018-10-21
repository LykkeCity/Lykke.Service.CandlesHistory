using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Common;
using Lykke.HttpClientGenerator;
using Lykke.Service.Assets.Client;
using Lykke.Service.CandleHistory.Repositories.Candles;
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
using StackExchange.Redis;
using Lykke.Logs.MsSql;
using MarginTrading.SettingsService.Contracts;

namespace Lykke.Service.CandlesHistory.DependencyInjection
{
    public class ApiModule : Module
    {
        private readonly IServiceCollection _services;
        private readonly MarketType _marketType;
        private readonly CandlesHistorySettings _settings;
        private readonly AssetsSettings _assetSettings;
        private readonly RedisSettings _redisSettings;
        private readonly IReloadingManager<string> _candleHistoryAssetConnection;
        private readonly ILog _log;

        public ApiModule(
            MarketType marketType,
            CandlesHistorySettings settings,
            AssetsSettings assetsSettings,
            RedisSettings redisSettings,
            IReloadingManager<string> candleHistoryAssetConnection,
            ILog log)
        {
            _marketType = marketType;
            _services = new ServiceCollection();
            _settings = settings;
            _assetSettings = assetsSettings;
            _redisSettings = redisSettings;
            _candleHistoryAssetConnection = candleHistoryAssetConnection;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<Clock>().As<IClock>();

            // For CandlesHistoryController
            builder.RegisterInstance(_settings.Db).AsSelf();

            RegisterResourceMonitor(builder);

            RegisterRedis(builder);

            RegisterAssets(builder);
            RegisterCandles(builder);

            builder.Populate(_services);
        }

        private void RegisterResourceMonitor(ContainerBuilder builder)
        {
            var monitorSettings = _settings.ResourceMonitor;

            if (monitorSettings != null)
                switch (monitorSettings.MonitorMode)
                {
                    case ResourceMonitorMode.Off:
                        // Do not register any resource monitor.
                        break;

                    case ResourceMonitorMode.AppInsightsOnly:
                        builder.RegisterResourcesMonitoring(_log);
                        break;

                    case ResourceMonitorMode.AppInsightsWithLog:
                        builder.RegisterResourcesMonitoringWithLogging(
                            _log,
                            monitorSettings.CpuThreshold,
                            monitorSettings.RamThreshold);
                        break;
                }
        }

        private void RegisterRedis(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var cm = ConnectionMultiplexer.Connect(_redisSettings.Configuration);
                cm.PreserveAsyncOrder = false;
                return cm;
            })
                .As<IConnectionMultiplexer>()
                .SingleInstance();
        }

        private void RegisterAssets(ContainerBuilder builder)
        {
            if (_marketType == MarketType.Spot)
            {
                _services.RegisterAssetsClient(AssetServiceSettings.Create(
                        new Uri(_assetSettings.ServiceUrl),
                        _settings.AssetsCache.ExpirationPeriod),
                    _log);

                builder.RegisterType<AssetPairsManager>()
                    .As<IAssetPairsManager>()
                    .SingleInstance();
            }
            else
            {
                builder.RegisterClient<IAssetPairsApi>(_assetSettings.ServiceUrl);

                builder.RegisterType<MtAssetPairsManager>()
                    .As<IAssetPairsManager>()
                    .SingleInstance();
            }
        }

        private void RegisterCandles(ContainerBuilder builder)
        {
            if (_settings.Db.StorageMode == StorageMode.SqlServer)
            {
                builder.RegisterType<SqlCandlesHistoryRepository>()
                    .As<ICandlesHistoryRepository>()
                    .WithParameter(TypedParameter.From(_candleHistoryAssetConnection))
                    .SingleInstance();
            }
            else if (_settings.Db.StorageMode == StorageMode.Azure)
            {
                builder.RegisterType<CandlesHistoryRepository>()
                    .As<ICandlesHistoryRepository>()
                    .WithParameter(TypedParameter.From(_candleHistoryAssetConnection))
                    .SingleInstance();
            }



            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .SingleInstance();

            builder.RegisterType<CandlesManager>()
                .As<ICandlesManager>()
                .SingleInstance();

            builder.RegisterType<RedisCandlesCacheService>()
                .As<ICandlesCacheService>()
                .WithParameter(TypedParameter.From(_marketType))
                .SingleInstance();

        }
    }
}
