using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services
{
    [UsedImplicitly]
    public class StartupManager : IStartupManager
    {
        private readonly ILog _log;
        private readonly ICandlesCacheService _candlesCacheService;

        public StartupManager(
            ILog log,
            ICandlesCacheService candlesCacheService)
        {
            _log = log.CreateComponentScope(nameof(StartupManager));
            _candlesCacheService = candlesCacheService;
        }

        public async Task StartAsync()
        {
            await _log.WriteInfoAsync(nameof(StartAsync), "", "Started up");
        }
    }
}
