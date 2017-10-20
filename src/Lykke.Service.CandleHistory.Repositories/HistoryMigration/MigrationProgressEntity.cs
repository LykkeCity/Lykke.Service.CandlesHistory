using System;
using Lykke.Domain.Prices;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.CandleHistory.Repositories.HistoryMigration
{
    public class MigrationProgressEntity : TableEntity
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

        public static MigrationProgressEntity Create(string assetPair, PriceType priceType, DateTime date)
        {
            return new MigrationProgressEntity
            {
                PartitionKey = GeneratePartitionKey(assetPair),
                RowKey = GenerateRowKey(priceType),
                Date = date
            };
        }
    }
}
