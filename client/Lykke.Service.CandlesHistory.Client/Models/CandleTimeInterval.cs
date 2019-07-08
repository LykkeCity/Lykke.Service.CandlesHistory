// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Lykke.Service.CandlesHistory.Client.Models
{
    using Lykke.Service;
    using Lykke.Service.CandlesHistory;
    using Lykke.Service.CandlesHistory.Client;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Runtime;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines values for CandleTimeInterval.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CandleTimeInterval
    {
        [EnumMember(Value = "Unspecified")]
        Unspecified,
        [EnumMember(Value = "Sec")]
        Sec,
        [EnumMember(Value = "Minute")]
        Minute,
        [EnumMember(Value = "Min5")]
        Min5,
        [EnumMember(Value = "Min15")]
        Min15,
        [EnumMember(Value = "Min30")]
        Min30,
        [EnumMember(Value = "Hour")]
        Hour,
        [EnumMember(Value = "Hour4")]
        Hour4,
        [EnumMember(Value = "Hour6")]
        Hour6,
        [EnumMember(Value = "Hour12")]
        Hour12,
        [EnumMember(Value = "Day")]
        Day,
        [EnumMember(Value = "Week")]
        Week,
        [EnumMember(Value = "Month")]
        Month
    }
    internal static class CandleTimeIntervalEnumExtension
    {
        internal static string ToSerializedValue(this CandleTimeInterval? value)  =>
            value == null ? null : ((CandleTimeInterval)value).ToSerializedValue();

        internal static string ToSerializedValue(this CandleTimeInterval value)
        {
            switch( value )
            {
                case CandleTimeInterval.Unspecified:
                    return "Unspecified";
                case CandleTimeInterval.Sec:
                    return "Sec";
                case CandleTimeInterval.Minute:
                    return "Minute";
                case CandleTimeInterval.Min5:
                    return "Min5";
                case CandleTimeInterval.Min15:
                    return "Min15";
                case CandleTimeInterval.Min30:
                    return "Min30";
                case CandleTimeInterval.Hour:
                    return "Hour";
                case CandleTimeInterval.Hour4:
                    return "Hour4";
                case CandleTimeInterval.Hour6:
                    return "Hour6";
                case CandleTimeInterval.Hour12:
                    return "Hour12";
                case CandleTimeInterval.Day:
                    return "Day";
                case CandleTimeInterval.Week:
                    return "Week";
                case CandleTimeInterval.Month:
                    return "Month";
            }
            return null;
        }

        internal static CandleTimeInterval? ParseCandleTimeInterval(this string value)
        {
            switch( value )
            {
                case "Unspecified":
                    return CandleTimeInterval.Unspecified;
                case "Sec":
                    return CandleTimeInterval.Sec;
                case "Minute":
                    return CandleTimeInterval.Minute;
                case "Min5":
                    return CandleTimeInterval.Min5;
                case "Min15":
                    return CandleTimeInterval.Min15;
                case "Min30":
                    return CandleTimeInterval.Min30;
                case "Hour":
                    return CandleTimeInterval.Hour;
                case "Hour4":
                    return CandleTimeInterval.Hour4;
                case "Hour6":
                    return CandleTimeInterval.Hour6;
                case "Hour12":
                    return CandleTimeInterval.Hour12;
                case "Day":
                    return CandleTimeInterval.Day;
                case "Week":
                    return CandleTimeInterval.Week;
                case "Month":
                    return CandleTimeInterval.Month;
            }
            return null;
        }
    }
}
