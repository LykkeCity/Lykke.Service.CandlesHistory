using System;

namespace Lykke.Service.CandlesHistory.Core.Extensions
{
    public static class RandomExtensions
    {
        public static decimal NextDecimal(this Random random, decimal minValue, decimal maxValue)
        {
            return (decimal)random.NextDouble() * (maxValue - minValue) + minValue;
        }
    }
}
