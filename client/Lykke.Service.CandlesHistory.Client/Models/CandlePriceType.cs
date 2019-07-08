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
        Mid,
        [EnumMember(Value = "Trades")]
        Trades
    }
    internal static class CandlePriceTypeEnumExtension
    {
        internal static string ToSerializedValue(this CandlePriceType? value)  =>
            value == null ? null : ((CandlePriceType)value).ToSerializedValue();

        internal static string ToSerializedValue(this CandlePriceType value)
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
                case CandlePriceType.Trades:
                    return "Trades";
            }
            return null;
        }

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
                case "Trades":
                    return CandlePriceType.Trades;
            }
            return null;
        }
    }
}
