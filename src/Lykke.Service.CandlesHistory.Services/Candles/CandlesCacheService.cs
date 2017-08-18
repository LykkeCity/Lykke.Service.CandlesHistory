using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Common;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesCacheService : ICandlesCacheService
    {
        private readonly int _amountOfCandlesToStore;
        /// <summary>
        /// Candles keyed by asset ID, price type and time interval type are sorted by DateTime 
        /// </summary>
        private readonly ConcurrentDictionary<string, LinkedList<IFeedCandle>> _candles;

        public CandlesCacheService(int amountOfCandlesToStore)
        {
            _amountOfCandlesToStore = amountOfCandlesToStore;
            _candles = new ConcurrentDictionary<string, LinkedList<IFeedCandle>>();
        }

        public void InitializeHistory(string assetPairId, TimeInterval timeInterval, PriceType priceType, IEnumerable<IFeedCandle> candles)
        {
            var key = GetKey(assetPairId, priceType, timeInterval);
            var candlesList = new LinkedList<IFeedCandle>(candles.Limit(_amountOfCandlesToStore));

            foreach (var candle in candlesList)
            {
                if (candle.DateTime.Kind != DateTimeKind.Utc)
                {
                    throw new InvalidOperationException($"Candle {candle.ToJson()} DateTime.Kind is {candle.DateTime.Kind}, but should be Utc");
                }
            }

            _candles.TryAdd(key, candlesList);
        }

        public IFeedCandle AddQuote(IQuote quote, PriceType priceType, TimeInterval timeInterval)
        {
            var key = GetKey(quote.AssetPair, priceType, timeInterval);
            IFeedCandle candle = null;

            _candles.AddOrUpdate(key,
                addValueFactory: k => AddNewCandlesHistory(quote, timeInterval, out candle),
                updateValueFactory: (k, hisotry) => UpdateCandlesHistory(hisotry, quote, timeInterval, out candle));

            if (candle == null)
            {
                throw new InvalidOperationException("No candle to return");
            }

            return candle;
        }

        public IEnumerable<IFeedCandle> GetCandles(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            if (fromMoment.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"Date kind should be Utc, but it is {fromMoment.Kind}", nameof(fromMoment));
            }
            if (toMoment.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"Date kind should be Utc, but it is {toMoment.Kind}", nameof(toMoment));
            }

            var key = GetKey(assetPairId, priceType, timeInterval);

            if (_candles.TryGetValue(key, out LinkedList<IFeedCandle> history))
            {
                IReadOnlyCollection<IFeedCandle> localHistory;
                lock (history)
                {
                    localHistory = history.ToArray();
                }

                // TODO: binnary search could increase speed
                return localHistory
                    .SkipWhile(i => i.DateTime < fromMoment)
                    .TakeWhile(i => i.DateTime < toMoment);
            }

            return Enumerable.Empty<IFeedCandle>();
        }

        public IFeedCandle GetMidPriceCandle(string assetPair, int assetPairAccuracy, DateTime timestamp, TimeInterval timeInterval)
        {
            try
            {
                var midKey = GetKey(assetPair, PriceType.Mid, timeInterval);

                IFeedCandle candle = null;

                var askCandle = TryGetCandle(assetPair, PriceType.Ask, timestamp, timeInterval);
                var bidCandle = TryGetCandle(assetPair, PriceType.Bid, timestamp, timeInterval);

                _candles.AddOrUpdate(midKey,
                    addValueFactory: k => AddNewMidCandlesHistory(askCandle, bidCandle, assetPairAccuracy, out candle),
                    updateValueFactory: (k, hisotry) => UpdateMidCandlesHistory(hisotry, askCandle, bidCandle,
                        assetPairAccuracy, out candle));

                if (candle == null)
                {
                    throw new InvalidOperationException("No candle to return");
                }

                return candle;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get mid candle for {new {assetPair, timestamp, timeInterval}.ToJson()}", ex);
            }
        }

        private LinkedList<IFeedCandle> AddNewMidCandlesHistory(
            IFeedCandle askCandle, 
            IFeedCandle bidCandle, 
            int assetPairAccuracy, 
            out IFeedCandle candle)
        {
            var history = new LinkedList<IFeedCandle>();

            candle = GenerateMidCandle(askCandle, bidCandle, assetPairAccuracy);

            history.AddLast(candle);

            return history;
        }

        private static IFeedCandle GenerateMidCandle(IFeedCandle askCandle, IFeedCandle bidCandle, int assetPairAccuracy)
        {
            if (askCandle == null && bidCandle == null)
            {
                throw new InvalidOperationException("At least one of ask or bid candle should exists in order to generate mid price candle");
            }

            if (askCandle != null && bidCandle != null)
            {
                return new AssetPairCandle
                {
                    DateTime = askCandle.DateTime,
                    Open = CalculateMidPrice(askCandle.Open, bidCandle.Open, assetPairAccuracy),
                    Close = CalculateMidPrice(askCandle.Close, bidCandle.Close, assetPairAccuracy),
                    Low = CalculateMidPrice(askCandle.Low, bidCandle.Low, assetPairAccuracy),
                    High = CalculateMidPrice(askCandle.High, bidCandle.High, assetPairAccuracy)
                };
            }

            if (askCandle != null)
            {
                return new AssetPairCandle
                {
                    DateTime = askCandle.DateTime,
                    Open = askCandle.Open,
                    Close = askCandle.Close,
                    Low = askCandle.Low,
                    High = askCandle.High
                };
            }

            return new AssetPairCandle
            {
                DateTime = bidCandle.DateTime,
                Open = bidCandle.Open,
                Close = bidCandle.Close,
                Low = bidCandle.Low,
                High = bidCandle.High
            };
        }

        private LinkedList<IFeedCandle> UpdateMidCandlesHistory(
            LinkedList<IFeedCandle> history, 
            IFeedCandle askCandle, 
            IFeedCandle bidCandle, 
            int assetPairAccuracy, 
            out IFeedCandle candle)
        {
            candle = GenerateMidCandle(askCandle, bidCandle, assetPairAccuracy);
            var intervalDate = askCandle?.DateTime ?? bidCandle.DateTime;

            lock (history)
            {
                // Starting from the latest candle, moving down to the history
                for (var node = history.Last; node != null; node = node.Previous)
                {
                    // Candle at given point already exists, so we merging they
                    if (node.Value.DateTime == intervalDate)
                    {
                        node.Value = candle;
                        break;
                    }

                    // If we found more early point than newCandle has,
                    // that's the point after which we should add newCandle
                    if (node.Value.DateTime < intervalDate)
                    {
                        history.AddAfter(node, candle);

                        // Should we remove oldest candle?
                        if (history.Count > _amountOfCandlesToStore)
                        {
                            history.RemoveFirst();
                        }

                        break;
                    }
                }
            }

            return history;
        }

        private static double CalculateMidPrice(double askPrice, double bidPrice, int assetPairAccuracy)
        {
            return Math.Round((askPrice + bidPrice) / 2, assetPairAccuracy, MidpointRounding.AwayFromZero);
        }

        private IFeedCandle TryGetCandle(string assetPair, PriceType priceTime, DateTime timestamp, TimeInterval timeInterval)
        {
            var askKey = GetKey(assetPair, priceTime, timeInterval);

            _candles.TryGetValue(askKey, out LinkedList<IFeedCandle> history);

            IFeedCandle[] localHistory = null;

            if (history != null)
            {
                lock (history)
                {
                    localHistory = history.ToArray();
                }
            }

            if (localHistory != null)
            {
                var candleDate = timestamp.RoundTo(timeInterval);

                return localHistory.FirstOrDefault(c => c.DateTime == candleDate);
            }

            return null;
        }

        private LinkedList<IFeedCandle> AddNewCandlesHistory(IQuote quote, TimeInterval timeInterval, out IFeedCandle candle)
        {
            var history = new LinkedList<IFeedCandle>();
            var intervalDate = quote.Timestamp.RoundTo(timeInterval);

            candle = quote.ToCandle(intervalDate);

            history.AddLast(candle);

            return history;
        }

        private LinkedList<IFeedCandle> UpdateCandlesHistory(LinkedList<IFeedCandle> history, IQuote quote, TimeInterval timeInterval, out IFeedCandle candle)
        {
            var intervalDate = quote.Timestamp.RoundTo(timeInterval);
            var newCandle = quote.ToCandle(intervalDate);

            candle = newCandle;

            lock (history)
            {
                // Starting from the latest candle, moving down to the history
                for (var node = history.Last; node != null; node = node.Previous)
                {
                    // Candle at given point already exists, so we merging they
                    if (node.Value.DateTime == newCandle.DateTime)
                    {
                        candle = node.Value.MergeWith(newCandle);
                        node.Value = candle;

                        break;
                    }

                    // If we found more early point than newCandle has,
                    // that's the point after which we should add newCandle
                    if (node.Value.DateTime < intervalDate)
                    {   
                        history.AddAfter(node, newCandle);

                        // Should we remove oldest candle?
                        if (history.Count > _amountOfCandlesToStore)
                        {
                            history.RemoveFirst();
                        }

                        break;
                    }
                }
            }

            if (candle == null)
            {
                throw new InvalidOperationException("No candle was merged or generated");
            }

            return history;
        }

        private static string GetKey(string assetPairId, PriceType priceType, TimeInterval timeInterval)
        {
            return $"{assetPairId.Trim().ToUpper()}-{priceType}-{timeInterval}";
        }
    }
}