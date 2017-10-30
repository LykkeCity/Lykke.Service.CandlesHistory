using System;
using System.Collections.Generic;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    /// <summary>
    /// Stores bid and ask sec candles for single asset pair
    /// </summary>
    public class BidAskHistoryService
    {
        private readonly LinkedList<(DateTime timestamp, ICandle ask, ICandle bid)> _storage;

        public BidAskHistoryService()
        {
            _storage = new LinkedList<(DateTime timestamp, ICandle ask, ICandle bid)>();
        }

        public IReadOnlyList<(DateTime timestamp, ICandle ask, ICandle bid)> PopReadyHistory(DateTime startDate)
        {
            lock (_storage)
            {
                var result = new List<(DateTime timestamp, ICandle ask, ICandle bid)>();

                for (var item = _storage.First; item != null;)
                {
                    var value = item.Value;

                    // Returns only items with both ask and bid candles

                    if (value.ask != null && value.bid != null)
                    {
                        result.Add(value);
                    }

                    // Breaks, when items filled with both ask and bid candles are ended, and they are after the startDate

                    else if(value.timestamp >= startDate)
                    {
                        break;
                    }

                    // Removes added to the result items and skipped due to timestamp is before startDate items

                    var itemToRemove = item;
                    item = item.Next;

                    _storage.Remove(itemToRemove);
                }

                return result;
            }
        }

        public void PushHistory(IEnumerable<ICandle> candles)
        {
            lock (_storage)
            {
                var item = _storage.First;

                // Assuming that both candles and _storage are sorted by timestamp

                foreach (var candle in candles)
                {
                    var found = false;

                    for(; item != null; item = item.Next)
                    {
                        var value = item.Value;
                        if (value.timestamp == candle.Timestamp)
                        {
                            item.Value = (
                                value.timestamp,
                                candle.PriceType == PriceType.Ask ? candle : value.ask,
                                candle.PriceType == PriceType.Bid ? candle : value.bid);
                            found = true;
                            break;
                        }

                        if (value.timestamp > candle.Timestamp)
                        {
                            break;
                        }
                    }

                    if (!found)
                    {
                        _storage.AddLast((
                            candle.Timestamp,
                            candle.PriceType == PriceType.Ask ? candle : null,
                            candle.PriceType == PriceType.Bid ? candle : null));
                    }
                }
            }
        }
    }
}
