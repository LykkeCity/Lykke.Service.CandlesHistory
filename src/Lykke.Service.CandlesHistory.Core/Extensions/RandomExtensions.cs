using System;

namespace Lykke.Service.CandlesHistory.Core.Extensions
{
    public static class RandomExtensions
    {
        public static double NextDouble(this Random random, double minValue, double maxValue)
        {
            return random.NextDouble() * (maxValue - minValue) + minValue;
        }

        public static int RandomSign(this Random rnd)
        {
            return rnd.Next(0, 1) * 2 - 1;
        }
    }
}
