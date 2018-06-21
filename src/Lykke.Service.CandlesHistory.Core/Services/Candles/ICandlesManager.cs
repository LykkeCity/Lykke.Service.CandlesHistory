using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesManager
    {
        Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment);

        /// <summary>
        /// Gets the summary trading volume and opposite trading volume for the specified asset pair.
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        /// <param name="interval">Candle time interval for the query.</param>
        /// <param name="ticksToPast">The amount of interval ticks into the past for summing volumes.</param>
        /// <returns>A tuple of values for TradingVolume and OppositeTradingVolume.</returns>
        /// <remarks>Please, note: you will get a summary trading volumes for the set of candles between 
        /// <see cref="DateTime.UtcNow"/> - <see cref="ticksToPast"/> (inclusive) and <see cref="DateTime.UtcNow"/> + 1 tick (exclusive).</remarks>
        Task<(decimal TradingVolume, decimal OppositeTradingVolume)> GetSummaryTradingVolumesAsync(string assetPairId, CandleTimeInterval interval, int ticksToPast);

        /// <summary>
        /// Gets the last trading price for the specified asset pair.
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        /// <param name="interval">Candle time interval for the query.</param>
        /// /// <param name="ticksToPast">The amount of interval ticks into the past (depth of search) for searching the last candle.</param>
        /// <returns>The last trade price.</returns>
        /// <remarks>Please, note: you will get the last trade price for the set of candles between 
        /// <see cref="DateTime.UtcNow"/> - <see cref="ticksToPast"/> (inclusive) and <see cref="DateTime.UtcNow"/> + 1 tick (exclusive).</remarks>
        Task<decimal> GetLastTradePriceAsync(string assetPairId, CandleTimeInterval interval, int ticksToPast);

        /// <summary>
        /// Gets the relative price change for the specified asset pair and period of time.
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        /// <param name="interval">Candle time interval for the query.</param>
        /// <param name="ticksToPast">The amount of interval ticks into the past for getting the begining and the final trade price.</param>
        /// <returns>The relative price change for trade candles.</returns>
        /// <remarks>The rule for calculating the price change is: (Close - Open) / Open, where Open is the first trade price for the period and
        /// Close is the last trade price.</remarks>
        Task<decimal> GetTradePriceChangeAsync(string assetPairId, CandleTimeInterval interval, int ticksToPast);

        Task<DateTime?> TryGetOldestCandleTimestampAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval interval);
    }
}
