using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services
{
    public class ShutdownManager : IShutdownManager
    {
        public bool IsShuttedDown { get; private set; }
        public bool IsShuttingDown { get; private set; }
        
        private readonly ILog _log;
        private readonly ICandlesCacheService _candlesCacheService;

        public ShutdownManager(
            ILog log,
            ICandlesCacheService candlesCacheService)
        {
            _log = log.CreateComponentScope(nameof(ShutdownManager));
            _candlesCacheService = candlesCacheService;
        }

        public async Task ShutdownAsync()
        {
            IsShuttingDown = true;

            await _log.WriteInfoAsync(nameof(ShutdownAsync), "", "Shutted down");

            IsShuttedDown = true;
            IsShuttingDown = false;
        }
    }
}
