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
using Lykke.Logs.MsSql.Extensions;

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    public class SqlAssetPairCandlesHistoryRepository
    {
        private const int commandTimeout = 150;

        private const string CreateTableScript = "CREATE TABLE {0}(" +
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
                                                 ", INDEX IX_{0} UNIQUE NONCLUSTERED (Timestamp, PriceType, TimeInterval));"; 

        private readonly string _tableName;
        private readonly string _connectionString;

        public SqlAssetPairCandlesHistoryRepository(string assetName, string connectionString, ILog log)
        {
            _connectionString = connectionString;
            _tableName = $"[Candles].[CandlesHistory_{assetName}]";

            using (var conn = new SqlConnection(_connectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, _tableName); }
                catch (Exception ex)
                {
                    log?.WriteErrorAsync(nameof(SqlAssetPairCandlesHistoryRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<ICandle>> GetCandlesAsync(CandlePriceType priceType, CandleTimeInterval interval, DateTime from, DateTime to)
        {

            var whereClause =
                "WHERE PriceType=@priceTypeVar AND TimeInterval=@intervalVar AND Timestamp >= @fromVar  AND Timestamp < @toVar";

            using (var conn = new SqlConnection(_connectionString))
            {

                    var objects = await conn.QueryAsync<SqlCandleHistoryItem>($"SELECT * FROM {_tableName} {whereClause}",
                        new { priceTypeVar = priceType, intervalVar = interval, fromVar = from, toVar = to }, null, commandTimeout: commandTimeout);

                    return objects;
               
            }

        }

        public async Task<ICandle> TryGetFirstCandleAsync(CandlePriceType priceType, CandleTimeInterval timeInterval)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var candle = await conn.QueryFirstOrDefaultAsync<SqlCandleHistoryItem>(
                    $"SELECT TOP(1) * FROM {_tableName} WHERE PriceType=@priceTypeVar AND TimeInterval=@intervalVar ",
                                                                    new { priceTypeVar = priceType, intervalVar = timeInterval });

                return candle;
            }
        }


    }
}
