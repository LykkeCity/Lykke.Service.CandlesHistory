// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using StackExchange.Redis;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    /// <summary>
    /// Caches candles in the redis using lexographical indexes with candles data as the auxiliary information of the index
    /// </summary>
    [UsedImplicitly]
    public class RedisCandlesCacheService : ICandlesCacheService
    {
        private const string TimestampFormat = "yyyyMMddHHmmss";

        private readonly IConnectionMultiplexer _multiplexer;
        private readonly MarketType _market;

        public RedisCandlesCacheService(IConnectionMultiplexer multiplexer, MarketType market)
        {
            _multiplexer = multiplexer;
            _market = market;
        }

        public IImmutableDictionary<string, IImmutableList<ICandle>> GetState()
        {
            throw new NotSupportedException();
        }

        public void SetState(IImmutableDictionary<string, IImmutableList<ICandle>> state)
        {
            throw new NotSupportedException();
        }

        public string DescribeState(IImmutableDictionary<string, IImmutableList<ICandle>> state)
        {
            throw new NotSupportedException();
        }
        
        public async Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            var key = GetKey(assetPairId, priceType, timeInterval);
            var from = fromMoment.ToString(TimestampFormat);
            var to = toMoment.ToString(TimestampFormat);
            var serializedValues = await _multiplexer.GetDatabase().SortedSetRangeByValueAsync(key, from, to, Exclude.Stop);
            
            return serializedValues.Select(v => DeserializeCandle(v, assetPairId, priceType, timeInterval));
        }

        private static ICandle DeserializeCandle(byte[] value, string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval)
        {
            // value is: 
            // 0 .. TimestampFormat.Length - 1 bytes: timestamp as yyyyMMddHHmmss in ASCII
            // TimestampFormat.Length .. end bytes: serialized RedistCachedCandle

            var timestampLength = TimestampFormat.Length;
            var timestampString = Encoding.ASCII.GetString(value, 0, timestampLength);
            var timestamp = DateTime.ParseExact(timestampString, TimestampFormat, CultureInfo.InvariantCulture);

            using (var stream = new MemoryStream(value, timestampLength, value.Length - timestampLength, writable: false))
            {
                var cachedCandle = MessagePack.MessagePackSerializer.Deserialize<RedisCachedCandle>(stream);

                return Candle.Create(
                    assetPairId,
                    priceType,
                    timeInterval,
                    timestamp,
                    cachedCandle.Open,
                    cachedCandle.Close,
                    cachedCandle.High,
                    cachedCandle.Low,
                    cachedCandle.TradingVolume,
                    cachedCandle.TradingOppositVolume,
                    cachedCandle.LastTradePrice,
                    cachedCandle.LastUpdateTimestamp);
            }
        }

        private string GetKey(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval)
        {
            return $"CandlesHistory:{_market}:{assetPairId}:{priceType}:{timeInterval}";
        }
    }
}
