using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.CandlesHistory.Core.Services;

namespace Lykke.Service.CandlesHistory.Services
{
    [UsedImplicitly]
    public class StartupManager : IStartupManager
    {
        public Task StartAsync()
        {
            return Task.CompletedTask;
        }
    }
}
