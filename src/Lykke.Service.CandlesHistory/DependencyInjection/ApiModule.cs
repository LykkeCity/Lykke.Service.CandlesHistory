using System;
using System.Collections.Immutable;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Lykke.AzureRepositories;
using Lykke.AzureRepositories.CandleHistory;
using Lykke.Domain.Prices.Repositories;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Repositories.Assets;
using Lykke.Service.CandlesHistory.Services;
using Lykke.Service.CandlesHistory.Services.Assets;
using Lykke.Service.CandlesHistory.Services.Candles;
using Microsoft.Extensions.DependencyInjection;

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
            builder.Register(c => new AssetPairsRepository(
                    new AzureTableStorage<AssetPairEntity>(_settings.CandlesHistory.Dictionaries.DbConnectionString, "Dictionaries", _log)))
                .As<IAssetPairsRepository>();

            builder.RegisterType<AssetPairsCacheService>()
                .As<IAssetPairsCacheService>()
                .SingleInstance();

            builder.RegisterType<AssetPairsManager>()
                .As<IAssetPairsManager>()
                .WithParameter(new TypedParameter(typeof(TimeSpan), _settings.CandlesHistory.Dictionaries.CacheExpirationPeriod))
                .SingleInstance();
        }

        private void RegisterCandles(ContainerBuilder builder)
        {
            var candlesHistoryAssetConnections = _settings.CandleHistoryAssetConnections.ToImmutableDictionary();

            _services.RegisterCandleHistoryRepository(candlesHistoryAssetConnections, _log);

            builder.Register(c => c.Resolve<CandleHistoryRepositoryResolver>())
                .As<ICandleHistoryRepository>();
            
            builder.RegisterType<CandlesBroker>()
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<MidPriceQuoteGenerator>()
                .As<IMidPriceQuoteGenerator>()
                .SingleInstance();

            builder.RegisterType<CandlesManager>()
                .As<ICandlesManager>()
                .As<IStartable>()
                .WithParameter(new TypedParameter(typeof(int), _settings.CandlesHistory.HistoryTicksCacheSize))
                .WithParameter(new TypedParameter(typeof(IImmutableDictionary<string, string>), candlesHistoryAssetConnections))
                .SingleInstance();

            builder.RegisterType<CachedCandlesHistoryService>()
                .As<ICachedCandlesHistoryService>()
                .WithParameter(new TypedParameter(typeof(int), _settings.CandlesHistory.HistoryTicksCacheSize))
                .SingleInstance();
        }
    }
}