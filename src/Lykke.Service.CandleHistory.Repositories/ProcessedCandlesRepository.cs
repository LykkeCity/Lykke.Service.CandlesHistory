using System;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.CandleHistory.Repositories
{
    public class ProcesssedCandleEntity : TableEntity, IProcessedCandle
    {
        public string AssetPair => PartitionKey;
        public DateTime Date { get; set; }

        public static string GeneratePartitionKey(string assetPair)
        {
            return assetPair;
        }

        public static string GenerateRowKey(PriceType priceType)
        {
            return priceType.ToString();
        }

        public static ProcesssedCandleEntity Create(string assetPair, PriceType priceType, DateTime date)
        {
            return new ProcesssedCandleEntity
            {
                PartitionKey = GeneratePartitionKey(assetPair),
                RowKey = GenerateRowKey(priceType),
                Date = date
            };
        }
    }

    public class ProcessedCandlesRepository : IProcessedCandlesRepository
    {
        private readonly INoSQLTableStorage<ProcesssedCandleEntity> _tableStorage;

        public ProcessedCandlesRepository(INoSQLTableStorage<ProcesssedCandleEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IProcessedCandle> GetProcessedCandleAsync(string assetPair, PriceType priceType)
        {
            return await _tableStorage.GetDataAsync(ProcesssedCandleEntity.GeneratePartitionKey(assetPair),
                ProcesssedCandleEntity.GenerateRowKey(priceType));
        }

        public async Task AddProcessedCandleAsync(string assetPair, PriceType priceType, DateTime date)
        {
            await _tableStorage.InsertOrReplaceAsync(ProcesssedCandleEntity.Create(assetPair, priceType, date));
        }
    }
}
