using System;
using System.Collections.Generic;

namespace Lykke.Service.CandlesHistory.Models.CandlesHistory
{
    public class CandlesHistoryResponseModel
    {
        public IEnumerable<Candle> History { get; set; }

        public class Candle
        {
            public DateTime DateTime { get; set; }
            public double Open { get; set; }
            public double Close { get; set; }
            public double High { get; set; }
            public double Low { get; set; }
        }
    }
}