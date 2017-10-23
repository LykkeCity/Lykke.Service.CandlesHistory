using System;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;

namespace Lykke.Service.CandleHistory.Repositories.HistoryMigration
{
    public class MigrationProgressRepository : IMigrationProgressRepository
    {
        private readonly INoSQLTableStorage<MigrationProgressEntity> _tableStorage;

        public MigrationProgressRepository(INoSQLTableStorage<MigrationProgressEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<DateTime?> GetProcessedDateAsync(string assetPair, PriceType priceType)
        {
            return (await _tableStorage.GetDataAsync(
                MigrationProgressEntity.GeneratePartitionKey(assetPair),
                MigrationProgressEntity.GenerateRowKey(priceType)))?.Date;
        }

        public Task SetProcessedDateAsync(string assetPair, PriceType priceType, DateTime date)
        {
            return Task.CompletedTask;
            // await _tableStorage.InsertOrReplaceAsync(MigrationProgressEntity.Create(assetPair, priceType, date));
        }

        public Task RemoveProcessedDateAsync(string assetPair)
        {
            return Task.CompletedTask;
            /*
            await Task.WhenAll(
                _tableStorage.DeleteIfExistAsync(
                    MigrationProgressEntity.GeneratePartitionKey(assetPair),
                    MigrationProgressEntity.GenerateRowKey(PriceType.Ask)),
                _tableStorage.DeleteIfExistAsync(
                    MigrationProgressEntity.GeneratePartitionKey(assetPair),
                    MigrationProgressEntity.GenerateRowKey(PriceType.Bid)),
                _tableStorage.DeleteIfExistAsync(
                    MigrationProgressEntity.GeneratePartitionKey(assetPair),
                    MigrationProgressEntity.GenerateRowKey(PriceType.Mid)));
                    */
        }
    }
}
