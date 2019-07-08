// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
