namespace Lykke.Service.CandlesHistory.Core
{
    public static class MathEx
    {
        /// <summary>
        /// Linear interpolation
        /// </summary>
        public static double Lerp(double v0, double v1, double t)
        {
            return (1 - t) * v0 + t * v1;
        }
    }
}
