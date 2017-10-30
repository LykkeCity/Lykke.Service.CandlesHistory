using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Common;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    public class CandleHistoryEntity : ITableEntity
    {
        public CandleHistoryEntity()
        {
        }

        public CandleHistoryEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            Candles = new List<CandleHistoryItem>();
        }

        #region ITableEntity properties

        public string ETag { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        #endregion

        public DateTime DateTime
        {
            get
            {
                // extract from RowKey + Interval from PKey
                if (!string.IsNullOrEmpty(RowKey))
                {
                    return ParseRowKey(RowKey);
                }
                return default(DateTime);
            }
        }

        public PriceType PriceType
        {
            get
            {
                if (!string.IsNullOrEmpty(PartitionKey))
                {
                    if (Enum.TryParse(PartitionKey, out PriceType value))
                    {
                        return value;
                    }
                }
                return PriceType.Unspecified;
            }
        }

        /// <summary>
        /// Candles, ordered by the tick
        /// </summary>
        public List<CandleHistoryItem> Candles { get; set; }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            EntityProperty property;
            if (properties.TryGetValue("Data", out property))
            {
                var json = property.StringValue;
                if (!string.IsNullOrEmpty(json))
                {
                    Candles = new List<CandleHistoryItem>(60);
                    Candles.AddRange(JsonConvert.DeserializeObject<IEnumerable<CandleHistoryItem>>(json));
                }
            }
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var dict = new Dictionary<string, EntityProperty>
            {
                {"Data", new EntityProperty(JsonConvert.SerializeObject(Candles))}
            };

            return dict;
        }

        public static string GeneratePartitionKey(PriceType priceType)
        {
            return $"{priceType}";
        }

        public static string GenerateRowKey(DateTime date, TimeInterval interval)
        {
            DateTime time;
            switch (interval)
            {
                case TimeInterval.Month:
                    time = new DateTime(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    break;

                case TimeInterval.Week:
                    time = DateTimeUtils.GetFirstWeekOfYear(date);
                    break;

                case TimeInterval.Day:
                    time = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    break;

                case TimeInterval.Hour12:
                case TimeInterval.Hour6:
                case TimeInterval.Hour4:
                case TimeInterval.Hour:
                    time = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
                    break;

                case TimeInterval.Min30:
                case TimeInterval.Min15:
                case TimeInterval.Min5:
                case TimeInterval.Minute:
                    time = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0, DateTimeKind.Utc);
                    break;

                case TimeInterval.Sec:
                    time = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, DateTimeKind.Utc);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(interval), interval, null);
            }

            return FormatRowKey(time);
        }

        public void MergeCandles(IEnumerable<ICandle> candles, TimeInterval timeInterval)
        {
            foreach (var candle in candles)
            {
                MergeCandle(candle, timeInterval);
            }
        }

        public void MergeCandle(ICandle candle, TimeInterval interval)
        {
            // 1. Check if candle with specified time already exist
            // 2. If found - merge, else - add to list

            var tick = candle.Timestamp.GetIntervalTick(interval);

            // Considering that Candles is ordered by Tick
            for(var i = 0; i < Candles.Count; ++i)
            {
                var currentCandle = Candles[i];

                // While currentCandle.Tick < tick - just skipping
                
                // That's it, merge to existing candle
                if (currentCandle.Tick == tick)
                {
                    currentCandle.InplaceMergeWith(candle);
                    return;
                }

                // No candle is found but there are some candles after, so we should insert candle right before them
                if (currentCandle.Tick > tick)
                {
                    Candles.Insert(i, candle.ToItem(tick));
                    return;
                }
            }

            // No candle is found, and no candles after, so just add to the end
            Candles.Add(candle.ToItem(tick));
        }

        private static string FormatRowKey(DateTime dateUtc)
        {
            return dateUtc.ToString("s"); // sortable format
        }

        private static DateTime ParseRowKey(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (DateTime.TryParseExact(value, "s", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal, out var date))
            {
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);
            }

            if (long.TryParse(value, out var ticks))
            {
                return new DateTime(ticks, DateTimeKind.Utc);
            }

            throw new InvalidOperationException($"Failed to parse RowKey '{value}' as DateTime");
        }
    }
}
