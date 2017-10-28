using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    /// <summary>
    /// Stores bid and ask sec candles for single asset pair
    /// </summary>
    public class BidAskHistoryService
    {
        private readonly ConcurrentDictionary<DateTime, (ICandle ask, ICandle bid)> _storage;

        public BidAskHistoryService()
        {
            _storage = new ConcurrentDictionary<DateTime, (ICandle ask, ICandle bid)>();
        }

        public IEnumerable<(DateTime timestamp, ICandle ask, ICandle bid)> GetHistory()
        {
            return _storage.Select(x => (x.Key, x.Value.ask, x.Value.bid));
        }

        public void SaveHistory(IEnumerable<ICandle> candles)
        {
            foreach (var candle in candles)
            {
                _storage.AddOrUpdate(
                    candle.Timestamp,
                    addValueFactory: k => (
                        candle.PriceType == PriceType.Ask ? candle : null,
                        candle.PriceType == PriceType.Bid ? candle : null),
                    updateValueFactory: (k, oldValue) => (
                        candle.PriceType == PriceType.Ask ? candle : oldValue.ask,
                        candle.PriceType == PriceType.Bid ? candle : oldValue.bid));
            }
        }
    }
}
