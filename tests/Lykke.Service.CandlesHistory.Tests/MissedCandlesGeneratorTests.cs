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
        public void Test_Generator()
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
                new DateTime(2017, 10, 28, 00, 00, 00, DateTimeKind.Utc),
                new DateTime(2017, 10, 28, 00, 01, 00, DateTimeKind.Utc),
                1,
                2,
                0.2);

            var a = candles.ToArray();
        }
    }
}
