using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    public class CandlesMigrationManager : IDisposable
    {
        public IReadOnlyDictionary<string, AssetPairMigrationHealthService> Health => _assetHealthServices;

        private readonly MigrationCandlesGenerator _candlesGenerator;
        private readonly MissedCandlesGenerator _missedCandlesGenerator;
        private readonly ICandlesMigrationService _candlesMigrationService;
        private readonly ICandlesPersistenceQueue _candlesPersistenceQueue;
        private readonly ICachedAssetsService _assetPairsManager;
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;
        private readonly ILog _log;
        private readonly Dictionary<string, AssetPairMigrationManager> _assetManagers;
        private readonly Dictionary<string, AssetPairMigrationHealthService> _assetHealthServices;

        public CandlesMigrationManager(
            MigrationCandlesGenerator candlesGenerator,
            MissedCandlesGenerator missedCandlesGenerator,
            ICandlesMigrationService candlesMigrationService, 
            ICandlesPersistenceQueue candlesPersistenceQueue,
            ICachedAssetsService cachedAssetsService,
            ICandlesHistoryRepository candlesHistoryRepository,
            ILog log)
        {
            _candlesGenerator = candlesGenerator;
            _missedCandlesGenerator = missedCandlesGenerator;
            _candlesMigrationService = candlesMigrationService;
            _candlesPersistenceQueue = candlesPersistenceQueue;
            _assetPairsManager = cachedAssetsService;
            _candlesHistoryRepository = candlesHistoryRepository;
            _log = log;

            _assetManagers = new Dictionary<string, AssetPairMigrationManager>();
            _assetHealthServices = new Dictionary<string, AssetPairMigrationHealthService>();
        }

        public async Task<string> RandomAsync(string assetPairId, DateTime start, DateTime end, double startPrice, double endPrice, double spread)
        {
            if (!_candlesHistoryRepository.CanStoreAssetPair(assetPairId))
            {
                return $"Connection string for the asset pair '{assetPairId}' not configuer";
            }

            var assetPair = await _assetPairsManager.TryGetAssetPairAsync(assetPairId);

            if (assetPair == null)
            {
                return $"Asset pair '{assetPairId}' not found";
            }

            lock (_assetManagers)
            {
                if (_assetManagers.ContainsKey(assetPairId))
                {
                    return $"{assetPairId} already being processed";
                }

                var assetHealthService = new AssetPairMigrationHealthService(_log);
                var assetManager = new AssetPairMigrationManager(
                    _candlesPersistenceQueue,
                    _candlesGenerator,
                    _missedCandlesGenerator,
                    assetHealthService,
                    assetPair,
                    _log,
                    _candlesMigrationService,
                    OnMigrationStopped);

                assetManager.StartRandom(start, end, startPrice, endPrice, spread);

                _assetHealthServices.Add(assetPairId, assetHealthService);
                _assetManagers.Add(assetPairId, assetManager);

                return $"{assetPairId} processing is started";
            }
        }

        public async Task<string> MigrateAsync(string assetPairId)
        {
            if (!_candlesHistoryRepository.CanStoreAssetPair(assetPairId))
            {
                return $"Connection string for the asset pair '{assetPairId}' not configuer";
            }

            var assetPair = await _assetPairsManager.TryGetAssetPairAsync(assetPairId);

            if (assetPair == null)
            {
                return $"Asset pair '{assetPairId}' not found";
            }

            lock (_assetManagers)
            {
                if (_assetManagers.ContainsKey(assetPairId))
                {
                    return $"{assetPairId} already being processed";
                }

                var assetHealthService = new AssetPairMigrationHealthService(_log);
                var assetManager = new AssetPairMigrationManager(
                    _candlesPersistenceQueue,
                    _candlesGenerator,
                    _missedCandlesGenerator,
                    assetHealthService,
                    assetPair, 
                    _log, 
                    _candlesMigrationService, 
                    OnMigrationStopped);

                assetManager.Start();

                _assetHealthServices.Add(assetPairId, assetHealthService);
                _assetManagers.Add(assetPairId, assetManager);
                
                 return $"{assetPairId} processing is started";
            }
        }

        public void Resume()
        {
            // TODO: Implement it if needed
        }

        private void OnMigrationStopped(string assetPair)
        {
            lock (_assetManagers)
            {
                _assetManagers.Remove(assetPair);
                _candlesGenerator.RemoveAssetPair(assetPair);
                _missedCandlesGenerator.RemoveAssetPair(assetPair);
            }
        }

        public void Stop()
        {
            lock (_assetManagers)
            {
                foreach (var pair in _assetManagers.Keys)
                    _assetManagers[pair].Stop();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
