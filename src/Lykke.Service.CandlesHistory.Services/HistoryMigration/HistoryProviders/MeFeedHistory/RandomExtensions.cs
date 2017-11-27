using System;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration.HistoryProviders.MeFeedHistory
{
    public static class RandomExtensions
    {
        public static decimal NextDecimal(this Random random, decimal minValue, decimal maxValue)
        {
            return (decimal)random.NextDouble() * (maxValue - minValue) + minValue;
        }
    }
}
