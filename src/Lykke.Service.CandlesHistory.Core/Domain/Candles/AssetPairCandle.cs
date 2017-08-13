using System;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public class AssetPairCandle : IFeedCandle
    {
        public string AssetPairId { get; set; }
        public PriceType PriceType { get; set; }
        public TimeInterval TimeInterval { get; set; }
        public DateTime DateTime { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public bool IsBuy { get; set; }
    }
}