﻿using System.Collections.Immutable;
using Lykke.Job.CandlesProducer.Contract;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public static class Constants
    {
        public static readonly ImmutableArray<CandleTimeInterval> StoredIntervals = ImmutableArray.Create
        (
            CandleTimeInterval.Min5,
            CandleTimeInterval.Min15,
            CandleTimeInterval.Min30,
            CandleTimeInterval.Hour,
            CandleTimeInterval.Hour4,
            CandleTimeInterval.Hour6,
            CandleTimeInterval.Hour12,
            CandleTimeInterval.Day,
            CandleTimeInterval.Week,
            CandleTimeInterval.Month
        );

        public static readonly ImmutableArray<CandlePriceType> StoredPriceTypes = ImmutableArray.Create
        (
            CandlePriceType.Ask,
            CandlePriceType.Bid,
            CandlePriceType.Mid,
            CandlePriceType.Trades
        );
    }
}
