using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Services
{
    public interface IShutdownManager
    {
        bool IsShuttedDown { get; }
        bool IsShuttingDown { get; }

        Task Shutdown();
    }
}