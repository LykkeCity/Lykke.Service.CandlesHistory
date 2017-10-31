using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Services;

namespace Lykke.Service.CandlesHistory.Services
{
    public interface ISnapshotSerializer
    {
        Task SerializeAsync<TState>(IHaveState<TState> stateHolder, ISnapshotRepository<TState> repository);
        Task<bool> DeserializeAsync<TState>(IHaveState<TState> stateHolder, ISnapshotRepository<TState> repository);
    }
}
