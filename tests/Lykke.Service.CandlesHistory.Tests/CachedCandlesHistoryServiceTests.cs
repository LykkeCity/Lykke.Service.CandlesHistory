using System;
using System.Linq;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Model;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Candles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Lykke.Service.CandlesHistory.Tests
{
    [TestClass]
    public class CachedCandlesCacheServiceTests
    {
        private const int AmountOfCandlesToStore = 5;

        private ICandlesCacheService _service;

        [TestInitialize]
        public void InitializeTest()
        {
            var logMock = new Mock<ILog>();
            _service = new CandlesCacheService(AmountOfCandlesToStore, logMock.Object);
        }

        
        #region History initialization

        [TestMethod]
        public void History_initializes_and_then_obtains()
        {
            // Arrange
            var history = new[]
            {
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc) },
            };

            // Act
            _service.Initialize("EURUSD", TimeInterval.Day, PriceType.Ask, history);

            var obtainedHistory = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc)).ToArray();
            
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
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 15, 0, 0, 0, DateTimeKind.Utc) },
            };

            // Act
            _service.Initialize("EURUSD", TimeInterval.Day, PriceType.Ask, history);

            var obtainedHistory = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc)).ToArray();

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
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc) },
            };

            // Act
            _service.Initialize("EURUSD", TimeInterval.Day, PriceType.Ask, history);

            var obtainedHistory = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc)).ToArray();

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
        public void Adding_candle_without_history_adds_candle()
        {
            // Arrange
            var candle = new FeedCandle { IsBuy = false, Open = 2, Close = 2, Low = 2, High = 2, DateTime = new DateTime(2017, 06, 23, 0, 0, 0, 0, DateTimeKind.Utc) };

            // Act
            _service.AddCandle(candle, "EURUSD", PriceType.Ask, TimeInterval.Day);

            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc)).ToArray();

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
        public void Adding_candle_merges_with_history()
        {
            // Arrange
            var history = new[]
            {
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc) }
            };
            var candle = new FeedCandle { IsBuy = false, Open = 2, Close = 2, Low = 2, High = 2, DateTime = new DateTime(2017, 06, 14, 0, 0, 0, 0, DateTimeKind.Utc) };

            _service.Initialize("EURUSD", TimeInterval.Day, PriceType.Ask, history);

            // Act
            _service.AddCandle(candle, "EURUSD", PriceType.Ask, TimeInterval.Day);

            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc)).ToArray();
            
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
        public void Sequental_candles_addition_without_history_merges_candles()
        {
            // Arrange
            var candle1 = new FeedCandle { IsBuy = false, Open = 2, Close = 2, Low = 2, High = 2, DateTime = new DateTime(2017, 06, 23, 0, 0, 0, 0, DateTimeKind.Utc) };
            var candle2 = new FeedCandle { IsBuy = false, Open = 1, Close = 1, Low = 1, High = 1, DateTime = new DateTime(2017, 06, 23, 0, 0, 0, 0, DateTimeKind.Utc) };
            var candle3 = new FeedCandle { IsBuy = false, Open = 5, Close = 5, Low = 5, High = 5, DateTime = new DateTime(2017, 06, 23, 0, 0, 0, 0, DateTimeKind.Utc) };
            var candle4 = new FeedCandle { IsBuy = false, Open = 4, Close = 4, Low = 4, High = 4, DateTime = new DateTime(2017, 06, 23, 0, 0, 0, 0, DateTimeKind.Utc) };

            // Act
            _service.AddCandle(candle1, "EURUSD", PriceType.Ask, TimeInterval.Day);
            _service.AddCandle(candle2, "EURUSD", PriceType.Ask, TimeInterval.Day);
            _service.AddCandle(candle3, "EURUSD", PriceType.Ask, TimeInterval.Day);
            _service.AddCandle(candle4, "EURUSD", PriceType.Ask, TimeInterval.Day);

            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc)).ToArray();

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
            var candle1 = new FeedCandle { IsBuy = false, Open = 2, Close = 2, Low = 2, High = 2, DateTime = new DateTime(2017, 06, 23, 13, 49, 23, 0, DateTimeKind.Utc) };
            var candle2 = new FeedCandle { IsBuy = false, Open = 1, Close = 1, Low = 1, High = 1, DateTime = new DateTime(2017, 06, 23, 13, 49, 24, 0, DateTimeKind.Utc) };
            var candle3 = new FeedCandle { IsBuy = false, Open = 5, Close = 5, Low = 5, High = 5, DateTime = new DateTime(2017, 06, 23, 13, 49, 25, 0, DateTimeKind.Utc) };
            var candle4 = new FeedCandle { IsBuy = false, Open = 4, Close = 4, Low = 4, High = 4, DateTime = new DateTime(2017, 06, 23, 13, 49, 26, 0, DateTimeKind.Utc) };
            var candle5 = new FeedCandle { IsBuy = false, Open = 3, Close = 3, Low = 3, High = 3, DateTime = new DateTime(2017, 06, 23, 13, 49, 27, 0, DateTimeKind.Utc) };
            var candle6 = new FeedCandle { IsBuy = false, Open = 7, Close = 7, Low = 7, High = 7, DateTime = new DateTime(2017, 06, 23, 13, 49, 28, 0, DateTimeKind.Utc) };

            // Act
            _service.AddCandle(candle1, "EURUSD", PriceType.Ask, TimeInterval.Sec);
            _service.AddCandle(candle2, "EURUSD", PriceType.Ask, TimeInterval.Sec);
            _service.AddCandle(candle3, "EURUSD", PriceType.Ask, TimeInterval.Sec);
            _service.AddCandle(candle4, "EURUSD", PriceType.Ask, TimeInterval.Sec);
            _service.AddCandle(candle5, "EURUSD", PriceType.Ask, TimeInterval.Sec);
            _service.AddCandle(candle6, "EURUSD", PriceType.Ask, TimeInterval.Sec);

            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Sec, new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc)).ToArray();

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
            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc));

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
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc) },
            };

            _service.Initialize("EURUSD", TimeInterval.Day, PriceType.Ask, history);

            // Act
            var candles = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc)).ToArray();

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
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc) },
            };

            _service.Initialize("EURUSD", TimeInterval.Day, PriceType.Ask, history);

            // Act
            var candles1 = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 05, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 05, 13, 0, 0, 0, DateTimeKind.Utc));
            var candles2 = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 07, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 13, 0, 0, 0, DateTimeKind.Utc));

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
                new FeedCandle { Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9, DateTime = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5, DateTime = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8, DateTime = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc) },
                new FeedCandle { Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7, DateTime = new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc) },
            };
            
            var candle = new FeedCandle { IsBuy = false, Open = 2, Close = 2, Low = 2, High = 2, DateTime = new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc) };

            _service.Initialize("EURUSD", TimeInterval.Day, PriceType.Ask, history);
            _service.AddCandle(candle, "EURUSD", PriceType.Ask, TimeInterval.Day);

            // Act
            var candles1 = _service.GetCandles("USDCHF", PriceType.Ask, TimeInterval.Day, new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc));
            var candles2 = _service.GetCandles("EURUSD", PriceType.Bid, TimeInterval.Day, new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc));
            var candles3 = _service.GetCandles("EURUSD", PriceType.Ask, TimeInterval.Sec, new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc));

            // Assert
            Assert.IsFalse(candles1.Any());
            Assert.IsFalse(candles2.Any());
            Assert.IsFalse(candles3.Any());
        }

        #endregion
    }
}