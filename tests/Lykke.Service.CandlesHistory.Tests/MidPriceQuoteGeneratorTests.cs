using System;
using Lykke.Domain.Prices.Model;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Candles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.Service.CandlesHistory.Tests
{
    [TestClass]
    public class MidPriceQuoteGeneratorTests
    {
        private IMidPriceQuoteGenerator _generator;

        [TestInitialize]
        public void InitializeTest()
        {
            _generator = new MidPriceQuoteGenerator();
        }

        [TestMethod]
        public void Bid_and_ask_quotes_generates_mid_quote()
        {
            // Arrange
            var date1 = new DateTime(2017, 06, 23, 12, 56, 00);
            var date2 = new DateTime(2017, 06, 23, 12, 56, 30);

            // Act
            _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 1, Timestamp = date1 }, 3);
            var mid = _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = true, Price = 2, Timestamp = date2 }, 3);

            // Assert
            Assert.IsNotNull(mid);
            Assert.AreEqual("EURUSD", mid.AssetPair);
            Assert.IsFalse(mid.IsBuy);
            Assert.AreEqual(1.5, mid.Price);
            Assert.AreEqual(date2, mid.Timestamp);
        }


        [TestMethod]
        public void Sequental_bid_and_ask_quotes_generates_new_mid_quote()
        {
            // Arrange
            var date1 = new DateTime(2017, 06, 23, 12, 56, 00);
            var date2 = new DateTime(2017, 06, 23, 12, 56, 30);
            var date3 = new DateTime(2017, 06, 23, 12, 57, 00);
            var date4 = new DateTime(2017, 06, 23, 12, 58, 00);

            // Act
            _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 1, Timestamp = date1 }, 3);
            _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = true, Price = 2, Timestamp = date2 }, 3);
            var mid2 = _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = true, Price = 3, Timestamp = date3 }, 3);
            var mid3 = _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 2, Timestamp = date4 }, 3);

            // Assert
            Assert.IsNotNull(mid2);
            Assert.AreEqual("EURUSD", mid2.AssetPair);
            Assert.IsFalse(mid2.IsBuy);
            Assert.AreEqual(2, mid2.Price);
            Assert.AreEqual(date3, mid2.Timestamp);

            Assert.IsNotNull(mid3);
            Assert.AreEqual("EURUSD", mid3.AssetPair);
            Assert.IsFalse(mid3.IsBuy);
            Assert.AreEqual(2.5, mid3.Price);
            Assert.AreEqual(date4, mid3.Timestamp);
        }

        [TestMethod]
        public void Mid_quote_price_is_rounded()
        {
            // Act
            _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 1.123, Timestamp = DateTime.UtcNow }, 2);
            var mid = _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = true, Price = 2, Timestamp = DateTime.UtcNow }, 2);

            // Assert
            Assert.AreEqual(1.56, mid.Price);
        }

        [TestMethod]
        public void Bid_only_quotes_not_generates_mid_quote()
        {
            // Act
            var mid1 = _generator.TryGenerate(new Quote {AssetPair = "EURUSD", IsBuy = true, Price = 1, Timestamp = DateTime.UtcNow}, 3);
            var mid2 = _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = true, Price = 1, Timestamp = DateTime.UtcNow }, 3);
            var mid3 = _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = true, Price = 1, Timestamp = DateTime.UtcNow }, 3);

            // Assert
            Assert.IsNull(mid1);
            Assert.IsNull(mid2);
            Assert.IsNull(mid3);
        }

        [TestMethod]
        public void Ask_only_quotes_not_generates_mid_quote()
        {
            // Act
            var mid1 = _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 1, Timestamp = DateTime.UtcNow }, 3);
            var mid2 = _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 1, Timestamp = DateTime.UtcNow }, 3);
            var mid3 = _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 1, Timestamp = DateTime.UtcNow }, 3);

            // Assert
            Assert.IsNull(mid1);
            Assert.IsNull(mid2);
            Assert.IsNull(mid3);
        }

        [TestMethod]
        public void Different_asset_pair_quotes_not_generates_mid_quote()
        {
            // Act
            var mid1 = _generator.TryGenerate(new Quote { AssetPair = "EURUSD", IsBuy = false, Price = 1, Timestamp = DateTime.UtcNow }, 3);
            var mid2 = _generator.TryGenerate(new Quote { AssetPair = "USDCHF", IsBuy = true, Price = 1, Timestamp = DateTime.UtcNow }, 3);
            var mid3 = _generator.TryGenerate(new Quote { AssetPair = "USDRUB", IsBuy = false, Price = 1, Timestamp = DateTime.UtcNow }, 3);

            // Assert
            Assert.IsNull(mid1);
            Assert.IsNull(mid2);
            Assert.IsNull(mid3);
        }
    }
}