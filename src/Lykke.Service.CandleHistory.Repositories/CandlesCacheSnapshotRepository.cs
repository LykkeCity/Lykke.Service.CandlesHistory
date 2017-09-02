using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Lykke.Service.CandleHistory.Repositories
{
    public class CandlesCacheSnapshotRepository : ISnapshotRepository<IImmutableDictionary<string, IImmutableList<ICandle>>>
    {
        private const string Container = "CacheSnapshot";
        private const string Key = "Singleton";

        private readonly IBlobStorage _storage;

        public CandlesCacheSnapshotRepository(IBlobStorage storage)
        {
            _storage = storage;
        }

        public async Task SaveAsync(IImmutableDictionary<string, IImmutableList<ICandle>> state)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BsonDataWriter(stream)
                {
                    DateTimeKindHandling = DateTimeKind.Utc
                })
                {
                    var serializer = new JsonSerializer();
                    var model = state.ToImmutableDictionary(i => i.Key, i => i.Value.Select(CandleSnapshotEntity.Create));

                    serializer.Serialize(writer, model);

                    await writer.FlushAsync();
                    await stream.FlushAsync();

                    stream.Seek(0, SeekOrigin.Begin);

                    await _storage.SaveBlobAsync(Container, Key, stream);
                }
            }
        }

        public async Task<IImmutableDictionary<string, IImmutableList<ICandle>>> TryGetAsync()
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
                    DateTimeKindHandling = DateTimeKind.Utc
                })
                {
                    var serializer = new JsonSerializer();
                    var model = serializer.Deserialize<ImmutableDictionary<string, CandleSnapshotEntity[]>>(reader);

                    return model.ToImmutableDictionary(i => i.Key, i => (IImmutableList<ICandle>)i.Value.ToImmutableList<ICandle>());
                }
            }
        }
    }
}