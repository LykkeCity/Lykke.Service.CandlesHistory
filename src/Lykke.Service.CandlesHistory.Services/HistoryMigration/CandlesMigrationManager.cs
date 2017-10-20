using System;
using System.Collections.Generic;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    public class CandlesMigrationManager : IDisposable
    {
        public IReadOnlyDictionary<string, AssetPairMigrationHealthService> Health => _assetHealthServices;

        private readonly MigrationCandlesGenerator _candlesGenerator;
        private readonly CandleMigrationService _candleMigrationService;
        private readonly ICandlesManager _candlesManager;
        private readonly ILog _log;
        private readonly Dictionary<string, AssetPairMigrationManager> _assetManagers;
        private readonly Dictionary<string, AssetPairMigrationHealthService> _assetHealthServices;

        public CandlesMigrationManager(
            MigrationCandlesGenerator candlesGenerator,
            CandleMigrationService candleMigrationService, 
            ICandlesManager candlesManager, 
            ILog log)
        {
            _candlesGenerator = candlesGenerator;
            _candleMigrationService = candleMigrationService;
            _candlesManager = candlesManager;
            _log = log;

            _assetManagers = new Dictionary<string, AssetPairMigrationManager>();
            _assetHealthServices = new Dictionary<string, AssetPairMigrationHealthService>();
        }

        public string Migrate(string assetPair)
        {
            lock (_assetManagers)
            {
                if (_assetManagers.ContainsKey(assetPair))
                {
                    return $"{assetPair} already being processed";
                }

                var assetHealthService = new AssetPairMigrationHealthService();
                var assetManager = new AssetPairMigrationManager(
                    _candlesGenerator,
                    assetHealthService,
                    assetPair, 
                    _log, 
                    _candleMigrationService, 
                    _candlesManager, 
                    OnMigrationStopped);

                assetManager.Start();

                _assetHealthServices.Add(assetPair, assetHealthService);
                _assetManagers.Add(assetPair, assetManager);
                
                 return $"{assetPair} processing is started";
            }
        }

        private void OnMigrationStopped(string assetPair)
        {
            lock (_assetManagers)
            {
                _assetManagers.Remove(assetPair);
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
