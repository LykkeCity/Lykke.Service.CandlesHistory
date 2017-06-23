using System;
using System.Linq;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Model;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Candles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.Service.CandlesHistory.Tests.CachedCandlesHistoryServiceTests
{
    [TestClass]
    public class CachedCandlesHistoryServiceInitializeHistoryTests
    {
        private const int AmountOfCandlesToStore = 5;

        private ICachedCandlesHistoryService _service;

        [TestInitialize]
        public void InitializeTest()
        {
            _service = new CachedCandlesHistoryService(AmountOfCandlesToStore);
        }

        
        #region History initialization

        [TestMethod]
        public void History_initializes_and_then_obtains()
        {
            // Arrange
            var history = new[]
            {
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14) },
            };

            // Act
            _service.InitializeHistory("EURUSD", PriceType.Ask, TimeInterval.Day, history);

            var obtainedHistory = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01), new DateTime(2017, 07, 01)).ToArray();
            
            // Assert
            Assert.AreEqual(5, obtainedHistory.Length);
            Assert.AreEqual(new DateTime(2017, 06, 10), obtainedHistory[0].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 11), obtainedHistory[1].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 12), obtainedHistory[2].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 13), obtainedHistory[3].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 14), obtainedHistory[4].DateTime);
        }

        [TestMethod]
        public void To_many_history_truncated_at_initialization()
        {
            // Arrange
            var history = new[]
            {
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 15) },
            };

            // Act
            _service.InitializeHistory("EURUSD", PriceType.Ask, TimeInterval.Day, history);

            var obtainedHistory = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01), new DateTime(2017, 07, 01)).ToArray();

            // Assert
            Assert.AreEqual(5, obtainedHistory.Length);
            Assert.AreEqual(new DateTime(2017, 06, 10), obtainedHistory[0].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 11), obtainedHistory[1].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 12), obtainedHistory[2].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 13), obtainedHistory[3].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 14), obtainedHistory[4].DateTime);
        }

        [TestMethod]
        public void Not_full_history_initializes()
        {
            // Arrange
            var history = new[]
            {
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13) },
            };

            // Act
            _service.InitializeHistory("EURUSD", PriceType.Ask, TimeInterval.Day, history);

            var obtainedHistory = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01), new DateTime(2017, 07, 01)).ToArray();

            // Assert
            Assert.AreEqual(4, obtainedHistory.Length);
            Assert.AreEqual(new DateTime(2017, 06, 10), obtainedHistory[0].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 11), obtainedHistory[1].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 12), obtainedHistory[2].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 13), obtainedHistory[3].DateTime);
        }

        #endregion


        #region Quote adding

        [TestMethod]
        public void Adding_quote_without_history_builds_candle()
        {
            // Arrange
            var quote = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 2, Timestamp = new DateTime(2017, 06, 23, 13, 49, 23, 432) };

            // Act
            _service.AddQuote(quote, PriceType.Ask, TimeInterval.Day);

            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01), new DateTime(2017, 07, 01)).ToArray();

            // Assert
            Assert.AreEqual(1, candles.Length);
            Assert.AreEqual(2, candles[0].Open);
            Assert.AreEqual(2, candles[0].Close);
            Assert.AreEqual(2, candles[0].Low);
            Assert.AreEqual(2, candles[0].High);
            Assert.AreEqual(false, candles[0].IsBuy);
            Assert.AreEqual(new DateTime(2017, 06, 23), candles[0].DateTime);
        }

        [TestMethod]
        public void Adding_quote_merges_with_history()
        {
            // Arrange
            var history = new[]
            {
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14) },
            };
            var quote = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 2, Timestamp = new DateTime(2017, 06, 14, 10, 50, 20) };

            _service.InitializeHistory("EURUSD", PriceType.Ask, TimeInterval.Day, history);

            // Act
            _service.AddQuote(quote, PriceType.Ask, TimeInterval.Day);

            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01), new DateTime(2017, 07, 01)).ToArray();
            
            // Assert
            Assert.AreEqual(5, candles.Length);

            Assert.AreEqual(1.6, candles[4].Open);
            Assert.AreEqual(2, candles[4].Close);
            Assert.AreEqual(1.4, candles[4].Low);
            Assert.AreEqual(2, candles[4].High);
            Assert.AreEqual(false, candles[4].IsBuy);
            Assert.AreEqual(new DateTime(2017, 06, 14), candles[4].DateTime);
        }

        [TestMethod]
        public void Sequental_quote_addition_without_history_merges_to_candle()
        {
            // Arrange
            var quote1 = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 2, Timestamp = new DateTime(2017, 06, 23, 13, 49, 23, 432) };
            var quote2 = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 1, Timestamp = new DateTime(2017, 06, 23, 13, 49, 24, 432) };
            var quote3 = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 5, Timestamp = new DateTime(2017, 06, 23, 13, 49, 25, 432) };
            var quote4 = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 4, Timestamp = new DateTime(2017, 06, 23, 13, 49, 26, 432) };

            // Act
            _service.AddQuote(quote1, PriceType.Ask, TimeInterval.Day);
            _service.AddQuote(quote2, PriceType.Ask, TimeInterval.Day);
            _service.AddQuote(quote3, PriceType.Ask, TimeInterval.Day);
            _service.AddQuote(quote4, PriceType.Ask, TimeInterval.Day);

            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01), new DateTime(2017, 07, 01)).ToArray();

            // Assert
            Assert.AreEqual(1, candles.Length);
            Assert.AreEqual(2, candles[0].Open);
            Assert.AreEqual(4, candles[0].Close);
            Assert.AreEqual(1, candles[0].Low);
            Assert.AreEqual(5, candles[0].High);
            Assert.AreEqual(false, candles[0].IsBuy);
            Assert.AreEqual(new DateTime(2017, 06, 23), candles[0].DateTime);
        }

        [TestMethod]
        public void To_old_candles_evicts()
        {
            // Arrange
            var quote1 = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 2, Timestamp = new DateTime(2017, 06, 23, 13, 49, 23, 432) };
            var quote2 = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 1, Timestamp = new DateTime(2017, 06, 23, 13, 49, 24, 432) };
            var quote3 = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 5, Timestamp = new DateTime(2017, 06, 23, 13, 49, 25, 432) };
            var quote4 = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 4, Timestamp = new DateTime(2017, 06, 23, 13, 49, 26, 432) };
            var quote5 = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 3, Timestamp = new DateTime(2017, 06, 23, 13, 49, 27, 432) };
            var quote6 = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 7, Timestamp = new DateTime(2017, 06, 23, 13, 49, 28, 432) };

            // Act
            _service.AddQuote(quote1, PriceType.Ask, TimeInterval.Sec);
            _service.AddQuote(quote2, PriceType.Ask, TimeInterval.Sec);
            _service.AddQuote(quote3, PriceType.Ask, TimeInterval.Sec);
            _service.AddQuote(quote4, PriceType.Ask, TimeInterval.Sec);
            _service.AddQuote(quote5, PriceType.Ask, TimeInterval.Sec);
            _service.AddQuote(quote6, PriceType.Ask, TimeInterval.Sec);

            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Sec, new DateTime(2017, 06, 01), new DateTime(2017, 07, 01)).ToArray();

            // Assert
            Assert.AreEqual(5, candles.Length);

            Assert.AreEqual(1, candles[0].Open);
            Assert.AreEqual(1, candles[0].Close);
            Assert.AreEqual(1, candles[0].Low);
            Assert.AreEqual(1, candles[0].High);
            Assert.AreEqual(false, candles[0].IsBuy);
            Assert.AreEqual(new DateTime(2017, 06, 23, 13, 49, 24), candles[0].DateTime);

            Assert.AreEqual(5, candles[1].Open);
            Assert.AreEqual(5, candles[1].Close);
            Assert.AreEqual(5, candles[1].Low);
            Assert.AreEqual(5, candles[1].High);
            Assert.AreEqual(false, candles[1].IsBuy);
            Assert.AreEqual(new DateTime(2017, 06, 23, 13, 49, 25), candles[1].DateTime);

            Assert.AreEqual(4, candles[2].Open);
            Assert.AreEqual(4, candles[2].Close);
            Assert.AreEqual(4, candles[2].Low);
            Assert.AreEqual(4, candles[2].High);
            Assert.AreEqual(false, candles[2].IsBuy);
            Assert.AreEqual(new DateTime(2017, 06, 23, 13, 49, 26), candles[2].DateTime);

            Assert.AreEqual(3, candles[3].Open);
            Assert.AreEqual(3, candles[3].Close);
            Assert.AreEqual(3, candles[3].Low);
            Assert.AreEqual(3, candles[3].High);
            Assert.AreEqual(false, candles[3].IsBuy);
            Assert.AreEqual(new DateTime(2017, 06, 23, 13, 49, 27), candles[3].DateTime);

            Assert.AreEqual(7, candles[4].Open);
            Assert.AreEqual(7, candles[4].Close);
            Assert.AreEqual(7, candles[4].Low);
            Assert.AreEqual(7, candles[4].High);
            Assert.AreEqual(false, candles[4].IsBuy);
            Assert.AreEqual(new DateTime(2017, 06, 23, 13, 49, 28), candles[4].DateTime);
        }

        #endregion


        #region Candles getting

        [TestMethod]
        public void Getting_empty_history_returns_empty_enumerable()
        {
            // Act
            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01), new DateTime(2017, 07, 01));

            // Assert
            Assert.IsNotNull(candles);
            Assert.IsFalse(candles.Any());
        }

        [TestMethod]
        public void Getting_filtered_by_from_and_to_moments_candles_returns_suitable_candles()
        {
            // Arrange
            var history = new[]
            {
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14) },
            };

            _service.InitializeHistory("EURUSD", PriceType.Ask, TimeInterval.Day, history);

            // Act
            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 11), new DateTime(2017, 06, 13)).ToArray();

            // Assert
            Assert.AreEqual(3, candles.Length);
            Assert.AreEqual(new DateTime(2017, 06, 11), candles[0].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 12), candles[1].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 13), candles[2].DateTime);
        }

        [TestMethod]
        public void Getting_filtered_by_from_and_to_moments_candles_returns_no_candles()
        {
            // Arrange
            var history = new[]
            {
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14) },
            };

            _service.InitializeHistory("EURUSD", PriceType.Ask, TimeInterval.Day, history);

            // Act
            var candles1 = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 05, 11), new DateTime(2017, 05, 13));
            var candles2 = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 07, 11), new DateTime(2017, 07, 13));

            // Assert
            Assert.IsFalse(candles1.Any());
            Assert.IsFalse(candles2.Any());
        }

        [TestMethod]
        public void Getting_candles_with_another_asset_pair_or_price_type_or_interval_returns_no_candles()
        {
            // Arrange
            var history = new[]
            {
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14) },
            };
            var quote = new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 2, Timestamp = new DateTime(2017, 06, 14, 10, 50, 20) };

            _service.InitializeHistory("EURUSD", PriceType.Ask, TimeInterval.Day, history);
            _service.AddQuote(quote, PriceType.Ask, TimeInterval.Day);

            // Act
            var candles1 = _service.GetCandles("USDCHF", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01), new DateTime(2017, 07, 01));
            var candles2 = _service.GetCandles("EURUSD", PriceType.Bid, TimeInterval.Day, new DateTime(2017, 06, 01), new DateTime(2017, 07, 01));
            var candles3 = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Sec, new DateTime(2017, 06, 01), new DateTime(2017, 07, 01));

            // Assert
            Assert.IsFalse(candles1.Any());
            Assert.IsFalse(candles2.Any());
            Assert.IsFalse(candles3.Any());
        }

        #endregion
    }
}