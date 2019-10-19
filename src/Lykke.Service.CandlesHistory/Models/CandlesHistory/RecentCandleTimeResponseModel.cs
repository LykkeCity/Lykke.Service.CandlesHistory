using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.CandlesHistory.Models.CandlesHistory
{
    public class RecentCandleTimeResponseModel
    {
        public bool Exists { get; set; }
        public DateTime ResultTimestamp { get; set; }
    }
}
