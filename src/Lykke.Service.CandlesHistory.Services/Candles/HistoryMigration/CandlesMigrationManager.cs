using System;
using System.Collections.Generic;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles.HistoryMigration
{
    public class CandlesMigrationManager : IDisposable
    {
        private readonly CandleMigrationService _candleMigrationService;
        private readonly ICandlesManager _candlesManager;
        private readonly ILog _log;
        private readonly Dictionary<string, AssetPairMigrationManager> _assetManagers = new Dictionary<string, AssetPairMigrationManager>();

        public CandlesMigrationManager(CandleMigrationService candleMigrationService, ICandlesManager candlesManager, ILog log)
        {
            _candleMigrationService = candleMigrationService;
            _candlesManager = candlesManager;
            _log = log;
        }

        public string Migrate(string assetPair)
        {
            lock (_assetManagers)
            {
                if (_assetManagers.ContainsKey(assetPair))
                {
                    return $"{assetPair} already being processed";
                }

                var assetManager = new AssetPairMigrationManager(
                    assetPair, 
                    _log, 
                    _candleMigrationService, 
                    _candlesManager, 
                    OnCompleteMigration);

                assetManager.Start();

                _assetManagers.Add(assetPair, assetManager);
                
                 return $"{assetPair} processing is started";
            }
        }

        private void OnCompleteMigration(string asssetPair)
        {
            lock (_assetManagers)
            {
                _assetManagers.Remove(asssetPair);
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
