using System;
using System.Linq;
using Lykke.Domain.Prices;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.CandlesHistory.Services.HistoryMigration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.Service.CandlesHistory.Tests
{
    [TestClass]
    public partial class MissedCandlesGeneratorTests
    {
        [TestMethod]
        public void Test_that_zero_zero_to_NaN_prices_generates_candles_well()
        {
            // Arrange
            var generator = new MissedCandlesGenerator();

            // Act
            var candles = generator.GenerateCandles(
                    new AssetPairResponseModel
                    {
                        Id = "EURUSD",
                        Accuracy = 5
                    },
                    PriceType.Ask,
                    new DateTime(2017, 08, 16, 15, 14, 49, DateTimeKind.Utc),
                    new DateTime(2017, 08, 16, 15, 14, 57, DateTimeKind.Utc),
                    0,
                    double.NaN,
                    0)
                .ToArray();

            // Assert
            Assert.AreEqual(7, candles.Length);
        }

        [TestMethod]
        public void Test_that_one_sec_candles_gap_generates_single_candle()
        {
            // Arrange
            var generator = new MissedCandlesGenerator();

            // Act
            var candles = generator.GenerateCandles(
                    new AssetPairResponseModel
                    {
                        Id = "BTCEUR",
                        Accuracy = 5
                    },
                    PriceType.Bid,
                    new DateTime(2016, 04, 28, 10, 57, 29, DateTimeKind.Utc),
                    new DateTime(2016, 04, 28, 10, 57, 31, DateTimeKind.Utc),
                    1,
                    2,
                    0.2)
                .ToArray();

            // Assert
            Assert.AreEqual(1, candles.Length);
            Assert.AreEqual(new DateTime(2016, 04, 28, 10, 57, 30, DateTimeKind.Utc), candles.First().Timestamp);
        }

        [TestMethod]
        public void Test_that_zero_candles_gap_generates_no_candles()
        {
            // Arrange
            var generator = new MissedCandlesGenerator();

            // Act
            var candles = generator.GenerateCandles(
                    new AssetPairResponseModel
                    {
                        Id = "BTCEUR",
                        Accuracy = 5
                    },
                    PriceType.Bid,
                    new DateTime(2016, 04, 28, 10, 57, 29, DateTimeKind.Utc),
                    new DateTime(2016, 04, 28, 10, 57, 30, DateTimeKind.Utc),
                    1,
                    2,
                    0.2)
                .ToArray();

            // Assert
            Assert.AreEqual(0, candles.Length);
        }
    }
}
