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
            [Required]
            public DateTime DateTime { get; set; }

            [Required]
            public double Open { get; set; }

            [Required]
            public double Close { get; set; }

            [Required]
            public double High { get; set; }

            [Required]
            public double Low { get; set; }
        }
    }
}