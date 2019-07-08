// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

            public double TradingVolume { get; set; }

            public double TradingOppositeVolume { get; set; }

            public double LastTradePrice { get; set; }

            public DateTime LastUpdateTimestamp { get; set; }
        }
    }
}
