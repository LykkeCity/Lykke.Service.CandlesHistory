using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Services;

namespace Lykke.Service.CandlesHistory.Services
{
    public class SnapshotSerializer<TState> : ISnapshotSerializer
    {
        private readonly IHaveState<TState> _stateHolder;
        private readonly ISnapshotRepository<TState> _repository;
        private readonly ILog _log;

        public SnapshotSerializer(
            IHaveState<TState> stateHolder,
            ISnapshotRepository<TState> repository,
            ILog log)
        {
            _stateHolder = stateHolder;
            _repository = repository;
            _log = log.CreateComponentScope($"{nameof(SnapshotSerializer<TState>)}[{_stateHolder.GetType().Name}]");
        }

        public async Task SerializeAsync()
        {
            await _log.WriteInfoAsync(nameof(SerializeAsync), "", "Gettings state...");

            var state = _stateHolder.GetState();

            await _log.WriteInfoAsync(nameof(SerializeAsync), _stateHolder.DescribeState(state), "Saving state...");

            await _repository.SaveAsync(state);

            await _log.WriteInfoAsync(nameof(SerializeAsync), "", "State saved");
        }

        public async Task<bool> DeserializeAsync()
        {
            await _log.WriteInfoAsync(nameof(DeserializeAsync), "", "Loading state...");

            var state = await _repository.TryGetAsync();

            if (state == null)
            {
                await _log.WriteWarningAsync("SnapshotSerializer", nameof(DeserializeAsync),
                    _stateHolder.GetType().Name, "No snapshot found to deserialize");

                return false;
            }

            await _log.WriteInfoAsync(nameof(DeserializeAsync), _stateHolder.DescribeState(state), "Settings state...");

            _stateHolder.SetState(state);

            await _log.WriteInfoAsync(nameof(DeserializeAsync), "", "State was set");

            return true;
        }
    }
}
