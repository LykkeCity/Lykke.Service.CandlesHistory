using System;
using System.Collections.Generic;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public class FailedCandlesEnvelope
    {
        public DateTime ProcessingMoment { get; set; }
        public string Exception { get; set; }
        public IEnumerable<ICandle> Candles { get; set; }
    }
}