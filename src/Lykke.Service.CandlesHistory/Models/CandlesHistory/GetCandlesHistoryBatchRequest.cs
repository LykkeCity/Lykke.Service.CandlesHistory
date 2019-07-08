// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Lykke.Job.CandlesProducer.Contract;

namespace Lykke.Service.CandlesHistory.Models.CandlesHistory
{
    public class GetCandlesHistoryBatchRequest
    {
        public string[] AssetPairs { get; set; }
        public CandlePriceType PriceType { get; set; }
        public CandleTimeInterval TimeInterval { get; set; }
        /// <summary>
        /// Inclusive from moment
        /// </summary>
        public DateTime FromMoment { get; set; }
        /// <summary>
        /// Exclusive to moment. If equals to the <see cref="FromMoment"/>, then exactly candle for exactly this moment will be returned
        /// </summary>
        public DateTime ToMoment { get; set; }
    }
}
