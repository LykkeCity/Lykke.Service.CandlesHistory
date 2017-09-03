using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Domain
{
    public interface ISnapshotRepository<TState>
    {
        Task SaveAsync(TState state);
        Task<TState> TryGetAsync();
    }
}