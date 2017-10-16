using System;
using System.Collections.Concurrent;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesMigrationManager : IDisposable
    {
        private readonly CandleMigrationService _candleMigrationService;
        private readonly ICandlesManager _candlesManager;
        private readonly ILog _log;
        private readonly ConcurrentDictionary<string, AssetPairMigrationManager> _assetManagers = new ConcurrentDictionary<string, AssetPairMigrationManager>();

        public CandlesMigrationManager(CandleMigrationService candleMigrationService, ICandlesManager candlesManager, ILog log)
        {
            _candleMigrationService = candleMigrationService;
            _candlesManager = candlesManager;
            _log = log;
        }

        public string Migrate(string assetPair)
        {
            if (_assetManagers.ContainsKey(assetPair))
                return $"already processing {assetPair}";

            var assetManager = new AssetPairMigrationManager(assetPair, _log, _candleMigrationService, _candlesManager, OnCompleteMigration);
            assetManager.Start();

            if (_assetManagers.TryAdd(assetPair, assetManager))
                return $"start processing {assetPair} candles";

            return "error migrating!";
        }

        private void OnCompleteMigration(string asssetPair)
        {
            _assetManagers.TryRemove(asssetPair, out _);
        }

        public void Stop()
        {
            foreach (var pair in _assetManagers.Keys)
                _assetManagers[pair].Stop();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
