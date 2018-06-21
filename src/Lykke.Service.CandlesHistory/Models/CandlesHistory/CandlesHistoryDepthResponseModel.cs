using System;
using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.CandlesHistory.Models.CandlesHistory
{
    public class CandlesHistoryDepthResponseModel
    {
        [Required]
        public string AssetPairId { get; set; }

        [Required]
        public DateTime? OldestAskTimestamp { get; set; }

        [Required]
        public DateTime? OldestBidTimestamp { get; set; }

        [Required]
        public DateTime? OldestMidTimestamp { get; set; }

        [Required]
        public DateTime? OldestTradesTimestamp { get; set; }
    }
}
