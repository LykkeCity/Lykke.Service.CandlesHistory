using System.Globalization;

namespace Lykke.Service.CandlesHistory.Core.Extensions
{
    public static class DoubleExtensions
    {
        public static int Places(this double val)
        {
            return val.ToString(CultureInfo.InvariantCulture).Split('.')[1].Length;
        }
    }
}
