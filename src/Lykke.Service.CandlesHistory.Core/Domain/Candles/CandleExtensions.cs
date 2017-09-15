﻿using System;
using System.Collections.Generic;
using Common;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public static class CandleExtensions
    {
        /// <summary>
        /// Merges all of candles placed in chronological order
        /// </summary>
        /// <param name="candles">Candles in hronological order</param>
        /// <param name="newTimestamp">
        /// <see cref="ICandle.Timestamp"/> of merged candle, if not specified, 
        /// then <see cref="ICandle.Timestamp"/> of all candles should be equals, 
        /// and it will be used as merged candle <see cref="ICandle.Timestamp"/>
        /// </param>
        /// <returns>Merged candle, or null, if no candles to merge</returns>
        public static ICandle MergeAll(this IEnumerable<ICandle> candles, DateTime? newTimestamp = null)
        {
            if (candles == null)
            {
                return null;
            }

            var open = 0d;
            var close = 0d;
            var high = 0d;
            var low = 0d;
            var assetPairId = string.Empty;
            var priceType = PriceType.Unspecified;
            var timeInterval = TimeInterval.Unspecified;
            var timestamp = DateTime.MinValue;
            var count = 0;

            using (var enumerator = candles.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var candle = enumerator.Current;

                    if (count == 0)
                    {
                        open = candle.Open;
                        close = candle.Close;
                        high = candle.High;
                        low = candle.Low;
                        assetPairId = candle.AssetPairId;
                        priceType = candle.PriceType;
                        timeInterval = candle.TimeInterval;
                        timestamp = candle.Timestamp;
                    }
                    else
                    {
                        if (assetPairId != candle.AssetPairId)
                        {
                            throw new InvalidOperationException($"Can't merge candles of different asset pairs. Current candle={candle.ToJson()}");
                        }

                        if (priceType != candle.PriceType)
                        {
                            throw new InvalidOperationException($"Can't merge candles of different price types. Current candle={candle.ToJson()}");
                        }

                        if (timeInterval != candle.TimeInterval)
                        {
                            throw new InvalidOperationException($"Can't merge candles of different time intervals. Current candle={candle.ToJson()}");
                        }

                        if (!newTimestamp.HasValue && timestamp != candle.Timestamp)
                        {
                            throw new InvalidOperationException($"Can't merge candles with different timestamps. Current candle={candle.ToJson()}");
                        }

                        close = candle.Close;
                        high = Math.Max(high, candle.High);
                        low = Math.Min(low, candle.Low);
                    }

                    count++;
                }
            }

            if (count > 0)
            {
                return new Candle
                {
                    Open = open,
                    Close = close,
                    High = high,
                    Low = low,
                    AssetPairId = assetPairId,
                    PriceType = priceType,
                    TimeInterval = timeInterval,
                    Timestamp = newTimestamp ?? timestamp
                };
            }

            return null;
        }
    }
}