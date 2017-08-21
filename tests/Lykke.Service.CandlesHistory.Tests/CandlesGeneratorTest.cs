using System;
using Lykke.Domain.Prices.Model;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Candles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.Service.CandlesHistory.Tests
{
    [TestClass]
    public class CandlesGeneratorTest
    {
        private ICandlesGenerator _generator;

        [TestInitialize]
        public void InitializeTests()
        {
            _generator = new CandlesGenerator();
        }

        [TestMethod]
        public void Bid_and_ask_candles_generates_mid_candle()
        {
            // Arrange/Act
            var midCandle = _generator.GenerateMidCandle(
                new FeedCandle { Open = 1, Close = 1, Low = 1, High = 1, DateTime = new DateTime(2017, 06, 23, 12, 56, 0, 0, DateTimeKind.Utc), IsBuy = false },
                new FeedCandle { Open = 2, Close = 2, Low = 2, High = 2, DateTime = new DateTime(2017, 06, 23, 12, 56, 0, 0, DateTimeKind.Utc), IsBuy = true },
                3);

            // Assert
            Assert.IsNotNull(midCandle);
            Assert.IsFalse(midCandle.IsBuy);
            Assert.AreEqual(1.5, midCandle.Open);
            Assert.AreEqual(1.5, midCandle.Close);
            Assert.AreEqual(1.5, midCandle.Low);
            Assert.AreEqual(1.5, midCandle.High);
            Assert.AreEqual(new DateTime(2017, 06, 23, 12, 56, 0, 0, DateTimeKind.Utc), midCandle.DateTime);
        }

        [TestMethod]
        public void Mid_candle_price_is_rounded()
        {
            // Arrange/Act
            var midCandle = _generator.GenerateMidCandle(
                new FeedCandle { Open = 1.123, Close = 1.123, Low = 1.123, High = 1.123, DateTime = new DateTime(2017, 06, 23, 12, 56, 0, 0, DateTimeKind.Utc), IsBuy = false },
                new FeedCandle { Open = 2, Close = 2, Low = 2, High = 2, DateTime = new DateTime(2017, 06, 23, 12, 56, 0, 0, DateTimeKind.Utc), IsBuy = true },
                2);

            // Assert
            Assert.IsNotNull(midCandle);
            Assert.AreEqual(1.56, midCandle.Open);
            Assert.AreEqual(1.56, midCandle.Close);
            Assert.AreEqual(1.56, midCandle.Low);
            Assert.AreEqual(1.56, midCandle.High);
        }

        [TestMethod]
        public void Bid_only_candle_generates_mid_candle()
        {
            // Arrange/Act
            var midCandle = _generator.GenerateMidCandle(
                null,
                new FeedCandle { Open = 2, Close = 2, Low = 2, High = 2, DateTime = new DateTime(2017, 06, 23, 12, 56, 0, 0, DateTimeKind.Utc), IsBuy = true },
                3);

            // Assert
            Assert.IsNotNull(midCandle);
            Assert.IsFalse(midCandle.IsBuy);
            Assert.AreEqual(2, midCandle.Open);
            Assert.AreEqual(2, midCandle.Close);
            Assert.AreEqual(2, midCandle.Low);
            Assert.AreEqual(2, midCandle.High);
            Assert.AreEqual(new DateTime(2017, 06, 23, 12, 56, 0, 0, DateTimeKind.Utc), midCandle.DateTime);
        }

        [TestMethod]
        public void Ask_only_candle_generates_mid_quote()
        {
            // Arrange/Act
            var midCandle = _generator.GenerateMidCandle(
                new FeedCandle { Open = 1, Close = 1, Low = 1, High = 1, DateTime = new DateTime(2017, 06, 23, 12, 56, 0, 0, DateTimeKind.Utc), IsBuy = false },
                null,
                3);

            // Assert
            Assert.IsNotNull(midCandle);
            Assert.IsFalse(midCandle.IsBuy);
            Assert.AreEqual(1, midCandle.Open);
            Assert.AreEqual(1, midCandle.Close);
            Assert.AreEqual(1, midCandle.Low);
            Assert.AreEqual(1, midCandle.High);
            Assert.AreEqual(new DateTime(2017, 06, 23, 12, 56, 0, 0, DateTimeKind.Utc), midCandle.DateTime);
        }
    }
}