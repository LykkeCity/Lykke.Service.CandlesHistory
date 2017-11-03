namespace Lykke.Service.CandlesHistory.Core
{
    public static class MathEx
    {
        /// <summary>
        /// Linear interpolation
        /// </summary>
        public static decimal Lerp(decimal v0, decimal v1, decimal t)
        {
            return (1m - t) * v0 + t * v1;
        }
    }
}
