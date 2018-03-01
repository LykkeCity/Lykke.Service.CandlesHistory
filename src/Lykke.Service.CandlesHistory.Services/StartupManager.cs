using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.CandlesHistory.Core.Services;

namespace Lykke.Service.CandlesHistory.Services
{
    [UsedImplicitly]
    public class StartupManager : IStartupManager
    {
        private readonly ILog _log;

        public StartupManager(
            ILog log)
        {
            _log = log.CreateComponentScope(nameof(StartupManager));
        }

        public async Task StartAsync()
        {
            await _log.WriteInfoAsync(nameof(StartAsync), "", "Started up");
        }
    }
}
