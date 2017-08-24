using AzureStorage;

namespace Lykke.Service.CandleHistory.Repositories
{
    public delegate INoSQLTableStorage<CandleTableEntity> CreateStorage(string assetPair, string tableName);
}