using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.CandleHistory.Repositories.Snapshots;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
using MessagePack;

namespace Lykke.Service.CandleHistory.Repositories.HistoryMigration.Snapshots
{
    public class MissedCandlesGeneratorSnapshotRepository : IMissedCandlesGeneratorSnapshotRepository
    {
        private const string Key = "MissedCandlesGenerator";

        private readonly IBlobStorage _storage;

        public MissedCandlesGeneratorSnapshotRepository(IBlobStorage storage)
        {
            _storage = storage;
        }

        public async Task SaveAsync(IImmutableDictionary<string, ICandle> state)
        {
            using (var stream = new MemoryStream())
            {
                var model = state.ToDictionary(i => i.Key, i => SnapshotCandleEntity.Create(i.Value));

                MessagePackSerializer.Serialize(stream, model);

                await stream.FlushAsync();
                stream.Seek(0, SeekOrigin.Begin);

                await _storage.SaveBlobAsync(Constants.SnapshotsContainer, Key, stream);
            }
        }

        public async Task<IImmutableDictionary<string, ICandle>> TryGetAsync()
        {
            if (!await _storage.HasBlobAsync(Constants.SnapshotsContainer, Key))
            {
                return null;
            }

            using (var stream = await _storage.GetAsync(Constants.SnapshotsContainer, Key))
            {
                var model = MessagePackSerializer.Deserialize<Dictionary<string, SnapshotCandleEntity>>(stream);

                return model.ToImmutableDictionary(i => i.Key, i => (ICandle)i.Value);
            }
        }
    }
}
