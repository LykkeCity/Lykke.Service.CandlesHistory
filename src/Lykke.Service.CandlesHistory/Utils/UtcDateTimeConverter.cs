using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.CandlesHistory.Utils
{
    /// <summary>
    /// Utility for converting DateTime properties to UTC
    /// </summary>
    public static class UtcDateTimeConverter
    {
        /// <summary>
        /// Converts a DateTime to Utc based on its DateTimeKind, Unspecified is assumed to be UTC
        /// </summary>
        public static DateTime ConvertToUtc(this DateTime dateTime)
        {
            switch (dateTime.Kind)
            {
                case DateTimeKind.Unspecified:
                    return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                default:
                    return dateTime.ToUniversalTime();
            }
        }
    }
}
