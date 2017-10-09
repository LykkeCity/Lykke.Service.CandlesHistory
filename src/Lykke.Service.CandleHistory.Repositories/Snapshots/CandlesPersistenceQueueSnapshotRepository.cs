using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using MessagePack;

namespace Lykke.Service.CandleHistory.Repositories.Snapshots
{
    public class CandlesPersistenceQueueSnapshotRepository : ISnapshotRepository<IImmutableList<ICandle>>
    {
        private const string Key = "CandlesPersistenceQueue";

        private readonly IBlobStorage _storage;

        public CandlesPersistenceQueueSnapshotRepository(IBlobStorage storage)
        {
            _storage = storage;
        }

        public async Task SaveAsync(IImmutableList<ICandle> state)
        {
            using (var stream = new MemoryStream())
            {
                var model = state.Select(SnapshotCandleEntity.Create);

                MessagePackSerializer.Serialize(stream, model);

                await stream.FlushAsync();
                stream.Seek(0, SeekOrigin.Begin);

                await _storage.SaveBlobAsync(Constants.SnapshotsContainer, Key, stream);
            }
        }

        public async Task<IImmutableList<ICandle>> TryGetAsync()
        {
            if (!await _storage.HasBlobAsync(Constants.SnapshotsContainer, Key))
            {
                return null;
            }

            using (var stream = await _storage.GetAsync(Constants.SnapshotsContainer, Key))
            {
                var model = MessagePackSerializer.Deserialize<IEnumerable<SnapshotCandleEntity>>(stream);

                return model.ToImmutableList<ICandle>();
            }
        }
    }
}
