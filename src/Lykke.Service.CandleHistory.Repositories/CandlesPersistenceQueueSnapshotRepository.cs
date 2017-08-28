using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Lykke.Service.CandleHistory.Repositories
{
    public class CandlesPersistenceQueueSnapshotRepository : ICandlesPersistenceQueueSnapshotRepository
    {
        private const string Container = "PersistenceQueueSnapshot";
        private const string Key = "Singleton";

        private readonly IBlobStorage _storage;

        public CandlesPersistenceQueueSnapshotRepository(IBlobStorage storage)
        {
            _storage = storage;
        }

        public async Task SaveAsync(AssetPairCandle[] state)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BsonDataWriter(stream))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(writer, state);

                    stream.Seek(0, SeekOrigin.Begin);

                    await _storage.SaveBlobAsync(Container, Key, stream);
                }
            }
        }

        public async Task<AssetPairCandle[]> TryGetAsync()
        {
            if (!await _storage.HasBlobAsync(Container, Key))
            {
                return null;
            }

            using (var stream = await _storage.GetAsync(Container, Key))
            {
                stream.Seek(0, SeekOrigin.Begin);

                using (var reader = new BsonDataReader(stream)
                {
                    ReadRootValueAsArray = true,
                    DateTimeKindHandling = DateTimeKind.Utc
                })
                {
                    var serializer = new JsonSerializer();

                    return serializer.Deserialize<AssetPairCandle[]>(reader);
                }
            }
        }
    }
}