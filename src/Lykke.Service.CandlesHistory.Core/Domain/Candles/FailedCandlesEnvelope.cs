using System;
using System.Collections.Generic;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public class FailedCandlesEnvelope
    {
        public DateTime ProcessingMoment { get; set; }
        public string Exception { get; set; }
        public string AssetPairId { get; set; }
        public PriceType PriceType { get; set; }
        public TimeInterval TimeInterval { get; set; }
        public IEnumerable<IFeedCandle> Candles { get; set; }
    }
}