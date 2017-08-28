using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface IShutdownManager
    {
        bool IsShuttedDown { get; }
        bool IsShuttingDown { get; }

        Task ShutdownAsync();
    }
}