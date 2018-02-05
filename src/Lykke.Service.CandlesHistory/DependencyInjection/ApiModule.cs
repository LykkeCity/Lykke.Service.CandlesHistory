using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Service.Assets.Client.Custom;
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

            builder.RegisterType<CandlesManager>()
                .As<ICandlesManager>()
                .SingleInstance();

            builder.RegisterType<CandlesCacheService>()
                .As<ICandlesCacheService>()
                .WithParameter(TypedParameter.From(_settings.HistoryTicksCacheSize))
                .SingleInstance();

            builder.RegisterType<CandlesCacheInitalizationService>()
                .WithParameter(TypedParameter.From(_settings.HistoryTicksCacheSize))
                .As<ICandlesCacheInitalizationService>();

            builder.RegisterType<CandlesHistorySizeValidator>()
                .AsSelf()
                .WithParameter(TypedParameter.From(_settings.MaxCandlesCountWhichCanBeRequested));
        }
    }
}
