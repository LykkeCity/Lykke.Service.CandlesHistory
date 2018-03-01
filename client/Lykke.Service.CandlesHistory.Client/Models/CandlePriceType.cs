﻿// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.CandlesHistory.Client.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines values for CandlePriceType.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CandlePriceType
    {
        [EnumMember(Value = "Unspecified")]
        Unspecified,
        [EnumMember(Value = "Bid")]
        Bid,
        [EnumMember(Value = "Ask")]
        Ask,
        [EnumMember(Value = "Mid")]
        Mid
    }
    // ReSharper disable once UnusedMember.Global
    internal static class CandlePriceTypeEnumExtension
    {
        // ReSharper disable once UnusedMember.Global
        internal static string ToSerializedValue(this CandlePriceType? value)  =>
            value?.ToSerializedValue();

        private static string ToSerializedValue(this CandlePriceType value)
        {
            switch( value )
            {
                case CandlePriceType.Unspecified:
                    return "Unspecified";
                case CandlePriceType.Bid:
                    return "Bid";
                case CandlePriceType.Ask:
                    return "Ask";
                case CandlePriceType.Mid:
                    return "Mid";
                default:
                    return null;
            }
        }

        // ReSharper disable once UnusedMember.Global
        internal static CandlePriceType? ParseCandlePriceType(this string value)
        {
            switch( value )
            {
                case "Unspecified":
                    return CandlePriceType.Unspecified;
                case "Bid":
                    return CandlePriceType.Bid;
                case "Ask":
                    return CandlePriceType.Ask;
                case "Mid":
                    return CandlePriceType.Mid;
            }
            return null;
        }
    }
}
