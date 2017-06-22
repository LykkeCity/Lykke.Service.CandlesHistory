using System;
using Common;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    internal static class DateTimeExtensions
    {
        public static DateTime RoundTo(this DateTime dateTime, TimeInterval interval)
        {
            switch (interval)
            {
                case TimeInterval.Month:
                    return dateTime.RoundToMonth();
                case TimeInterval.Week:
                    return dateTime.RoundToWeek();
                case TimeInterval.Day:
                    return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Kind);
                case TimeInterval.Hour12:
                    return dateTime.RoundToHour(12);
                case TimeInterval.Hour6:
                    return dateTime.RoundToHour(6);
                case TimeInterval.Hour4:
                    return dateTime.RoundToHour(4);
                case TimeInterval.Hour:
                    return dateTime.RoundToHour();
                case TimeInterval.Min30:
                    return dateTime.RoundToMinute(30);
                case TimeInterval.Min15:
                    return dateTime.RoundToMinute(15);
                case TimeInterval.Min5:
                    return dateTime.RoundToMinute(5);
                case TimeInterval.Minute:
                    return dateTime.RoundToMinute();
                case TimeInterval.Sec:
                    return dateTime.TruncMiliseconds();
                default:
                    throw new ArgumentOutOfRangeException(nameof(interval), interval, "Unexpected TimeInterval value.");
            }
        }
    }
}