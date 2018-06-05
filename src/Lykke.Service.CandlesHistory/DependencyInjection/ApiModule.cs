﻿using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Common;
using Lykke.Service.Assets.Client;
using Lykke.Service.CandleHistory.Repositories.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services;
using Lykke.Service.CandlesHistory.Services.Assets;
using Lykke.Service.CandlesHistory.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Settings;
using Lykke.Service.CandlesHistory.Validation;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Lykke.Service.CandlesHistory.DependencyInjection
{
    public class ApiModule : Module
    {
        private readonly IServiceCollection _services;
        private readonly MarketType _marketType;
        private readonly CandlesHistorySettings _settings;
        private readonly AssetsSettings _assetSettings;
        private readonly RedisSettings _redisSettings;
        private readonly IReloadingManager<Dictionary<string, string>> _candleHistoryAssetConnections;
        private readonly ILog _log;

        public ApiModule(MarketType marketType,
            CandlesHistorySettings settings,
            AssetsSettings assetSettings,
            RedisSettings redisSettings,
            IReloadingManager<Dictionary<string, string>> candleHistoryAssetConnections,
            ILog log)
        {
            _marketType = marketType;
            _services = new ServiceCollection();
            _settings = settings;
            _assetSettings = assetSettings;
            _redisSettings = redisSettings;
            _candleHistoryAssetConnections = candleHistoryAssetConnections;
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

            RegisterResourceMonitor(builder);

            RegisterRedis(builder);

            RegisterAssets(builder);
            RegisterCandles(builder);

            builder.Populate(_services);
        }

        private void RegisterResourceMonitor(ContainerBuilder builder)
        {
            var monitorSettings = _settings.ResourceMonitor;

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
            _services.RegisterAssetsClient(AssetServiceSettings.Create(
                    new Uri(_assetSettings.ServiceUrl), 
                    _settings.Assets.AssetsCacheExpirationPeriod),
                _log);

                builder.RegisterType<AssetPairsManager>().As<IAssetPairsManager>();
        }

        private void RegisterCandles(ContainerBuilder builder)
        {
            builder.RegisterType<CandlesHistoryRepository>()
                .As<ICandlesHistoryRepository>()
                .WithParameter(TypedParameter.From(_candleHistoryAssetConnections))
                .SingleInstance();

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

            builder.RegisterType<CandlesHistorySizeValidator>()
                .AsSelf()
                .WithParameter(TypedParameter.From(_settings.MaxCandlesCountWhichCanBeRequested));
        }
    }
}
