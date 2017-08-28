using System.Collections.Generic;
using System.Collections.Immutable;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class Constants
    {
        public static readonly ImmutableArray<TimeInterval> StoredIntervals = ImmutableArray.Create
        (
            TimeInterval.Sec,
            TimeInterval.Minute,
            TimeInterval.Min30,
            TimeInterval.Hour,
            TimeInterval.Day,
            TimeInterval.Week,
            TimeInterval.Month
        );

        public static readonly ImmutableArray<PriceType> StoredPriceTypes = ImmutableArray.Create
        (
            PriceType.Ask,
            PriceType.Bid,
            PriceType.Mid
        );
    }
}