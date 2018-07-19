using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandleHistory.Repositories.Extensions;

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    public class SqlAssetPairCandlesHistoryRepository
    {
        private const int commandTimeout = 150;
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 "[Id] [bigint] NOT NULL IDENTITY(1,1) PRIMARY KEY," +
                                                 "[AssetPairId] [nvarchar] (64) NOT NULL, " +
                                                 "[PriceType] [int] NOT NULL ," +
                                                 "[Open] [float] NOT NULL, " +
                                                 "[Close] [float] NOT NULL, " +
                                                 "[High] [float] NOT NULL, " +
                                                 "[Low] [float] NOT NULL, " +
                                                 "[TimeInterval] [int] NOT NULL, " +
                                                 "[TradingVolume] [float] NOT NULL, " +
                                                 "[TradingOppositeVolume] [float] NOT NULL, " +
                                                 "[LastTradePrice] [float] NOT NULL, " +
                                                 "[Timestamp] [datetime] NULL, " +
                                                 "[LastUpdateTimestamp] [datetime] NULL" +
                                                 ", INDEX IX_{0} NONCLUSTERED (Timestamp, PriceType, TimeInterval)" +
                                                 ", CONSTRAINT UC_Person UNIQUE (PriceType, TimeInterval, Timestamp));";

        private static Type DataType => typeof(ICandle);
        private static readonly string GetColumns = "[" + string.Join("],[", DataType.GetProperties().Select(x => x.Name)) + "]";
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));
        private static readonly string GetUpdateClause = string.Join(",",
            DataType.GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly string assetName;
        private readonly string TableName;
        private readonly string _connectionString;
        private readonly ILog _log;

        public SqlAssetPairCandlesHistoryRepository(string assetName, string connectionString, ILog log)
        {
            _log = log;
            _connectionString = connectionString;
            TableName = assetName + "_candleshistory";

            using (var conn = new SqlConnection(_connectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, assetName + "_candleshistory"); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(SqlAssetPairCandlesHistoryRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<ICandle>> GetCandlesAsync(CandlePriceType priceType, CandleTimeInterval interval, DateTime from, DateTime to)
        {

            var whereClause =
                "WHERE PriceType=@priceTypeVar AND TimeInterval=@intervalVar AND Timestamp >= @fromVar  AND Timestamp <= @toVar";

            using (var conn = new SqlConnection(_connectionString))
            {

                    var objects = await conn.QueryAsync<Candle>($"SELECT * FROM {TableName} {whereClause}",
                        new { priceTypeVar = priceType, intervalVar = interval, fromVar = from, toVar = to }, null, commandTimeout: commandTimeout);

                    return objects;
               
            }

        }

        public async Task<ICandle> TryGetFirstCandleAsync(CandlePriceType priceType, CandleTimeInterval timeInterval)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var candle = await conn.QueryAsync<ICandle>(
                    $"SELECT TOP(1) * FROM {TableName} WHERE PriceType=@priceTypeVar AND TimeInterval=@intervalVar ",
                                                                    new { priceTypeVar = priceType, intervalVar = timeInterval });

                return candle.FirstOrDefault();
            }
        }


    }
}
