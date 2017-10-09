using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Lykke.Service.CandleHistory.Repositories.Snapshots.Legacy
{
    [Obsolete("Used for snapshot migration")]
    public class LegacyCandlesPersistenceQueueSnapshotRepository : ISnapshotRepository<IImmutableList<ICandle>>
    {
        private const string Container = "PersistenceQueueSnapshot";
        private const string Key = "Singleton";

        private readonly IBlobStorage _storage;

        public LegacyCandlesPersistenceQueueSnapshotRepository(IBlobStorage storage)
        {
            _storage = storage;
        }

        public Task SaveAsync(IImmutableList<ICandle> state)
        {
            throw new NotImplementedException();
        }

        public async Task<IImmutableList<ICandle>> TryGetAsync()
        {
            if (!await _storage.HasBlobAsync(Container, Key))
            {
                return null;
            }

            using (var stream = await _storage.GetAsync(Container, Key))
            {
                await stream.FlushAsync();

                stream.Seek(0, SeekOrigin.Begin);

                using (var reader = new BsonDataReader(stream)
                {
                    ReadRootValueAsArray = true,
                    DateTimeKindHandling = DateTimeKind.Utc
                })
                {
                    var serializer = new JsonSerializer();
                    var model = serializer.Deserialize<IEnumerable<LegacySnapshotCandleEntity>>(reader);

                    return model.ToImmutableList<ICandle>();
                }
            }
        }
    }
}
