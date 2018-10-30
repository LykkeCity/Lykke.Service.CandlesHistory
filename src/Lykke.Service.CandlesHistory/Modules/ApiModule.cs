using System;
using System.Collections.Generic;
using Autofac;
using JetBrains.Annotations;
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
using Lykke.SettingsReader;
using StackExchange.Redis;

namespace Lykke.Service.CandlesHistory.Modules
{
    [UsedImplicitly]
    public class ApiModule : Module
    {
        private readonly MarketType _marketType;
        private readonly CandlesHistorySettings _settings;
        private readonly AssetsSettings _assetSettings;
        private readonly RedisSettings _redisSettings;
        private readonly IReloadingManager<Dictionary<string, string>> _candleHistoryAssetConnections;

        public ApiModule(IReloadingManager<AppSettings> settings)
        {
            _marketType = settings.CurrentValue.CandlesHistory != null
                ? MarketType.Spot
                : MarketType.Mt;
            
            _settings = settings.CurrentValue.CandlesHistory ?? settings.CurrentValue.MtCandlesHistory;
            
            _assetSettings = settings.CurrentValue.Assets;
            _redisSettings = settings.CurrentValue.RedisSettings;
            _candleHistoryAssetConnections = settings.CurrentValue.CandleHistoryAssetConnections != null
                ? settings.Nested(x => x.CandleHistoryAssetConnections)
                : settings.Nested(x => x.MtCandleHistoryAssetConnections);
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Clock>().As<IClock>();

            // For CandlesHistoryController
            builder.RegisterInstance(_candleHistoryAssetConnections.CurrentValue)
                .AsSelf();

            RegisterResourceMonitor(builder);

            RegisterRedis(builder);

            RegisterAssets(builder);
            RegisterCandles(builder);
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
                    builder.RegisterResourcesMonitoring();
                    break;

                case ResourceMonitorMode.AppInsightsWithLog:
                    builder.RegisterResourcesMonitoringWithLogging(
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
            builder.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_assetSettings.ServiceUrl),
                _settings.AssetsCache.ExpirationPeriod));

            builder.RegisterType<AssetPairsManager>()
                .As<IAssetPairsManager>();
        }

        private void RegisterCandles(ContainerBuilder builder)
        {
            builder.RegisterType<CandlesHistoryRepository>()
                .As<ICandlesHistoryRepository>()
                .WithParameter(TypedParameter.From(_candleHistoryAssetConnections))
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
