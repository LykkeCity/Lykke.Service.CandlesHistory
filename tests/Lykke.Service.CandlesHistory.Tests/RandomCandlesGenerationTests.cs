using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration;
using Lykke.Service.CandlesHistory.Services.HistoryMigration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Lykke.Service.CandlesHistory.Tests
{
    [TestClass]
    public class RandomCandlesGenerationTests
    {
        private Mock<ICandlesPersistenceQueue> _persistenceQueueMock;
        private Mock<ICandlesHistoryRepository> _candlesHistoryRepositoryMock;
        private Mock<ICandlesMigrationService> _migrationServiceMock;
        private Mock<ICachedAssetsService> _cachedAssetServiceMock;
        private CandlesMigrationManager _manager;

        [TestInitialize]
        public void InitializeTest()
        {
            var logMock = new Mock<ILog>();

            _persistenceQueueMock = new Mock<ICandlesPersistenceQueue>();
            _migrationServiceMock = new Mock<ICandlesMigrationService>();
            _cachedAssetServiceMock = new Mock<ICachedAssetsService>();
            _candlesHistoryRepositoryMock = new Mock<ICandlesHistoryRepository>();

            _cachedAssetServiceMock
                .Setup(x => x.TryGetAssetPairAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Func<string, CancellationToken, IAssetPair>((a, c) => new AssetPairResponseModel
                {
                    Id = a,
                    Accuracy = 5,
                    Name = a
                }));

            _candlesHistoryRepositoryMock
                .Setup(x => x.CanStoreAssetPair(It.IsAny<string>()))
                .Returns(true);

            _manager = new CandlesMigrationManager(
                new MigrationCandlesGenerator(),
                new MissedCandlesGenerator(),
                _migrationServiceMock.Object,
                _persistenceQueueMock.Object,
                _cachedAssetServiceMock.Object,
                _candlesHistoryRepositoryMock.Object,
                logMock.Object);
        }

        [TestMethod]
        public async Task Test_that_random_day_generation_generates_all_candles()
        {
            // Arrange
            var persistedCandleCounter = new Dictionary<(TimeInterval, PriceType), int>();

            _persistenceQueueMock
                .Setup(x => x.EnqueueCandle(It.IsAny<ICandle>()))
                .Callback<ICandle>(candle =>
                {
                    var counterKey = (candle.TimeInterval, candle.PriceType);
                    if (!persistedCandleCounter.TryGetValue(counterKey, out var counter))
                    {
                        persistedCandleCounter.Add(counterKey, 1);
                    }
                    else
                    {
                        persistedCandleCounter[counterKey] = counter + 1;
                    }
                });

            // Act
            await _manager.RandomAsync(
                "EURUSD",
                new DateTime(2017, 10, 25, 00, 00, 00, DateTimeKind.Utc).AddSeconds(-1),
                new DateTime(2017, 10, 26, 00, 00, 00, DateTimeKind.Utc),
                1.3212,
                1.1721,
                0.02);

            while (true)
            {
                if (_manager.Health["EURUSD"].OverallProgressHistory.Any(x => x.Progress.Contains("Done")))
                {
                    break;
                }

                if (_manager.Health["EURUSD"].OverallProgressHistory.Any(x => x.Progress.Contains("Failed")))
                {
                    Assert.Fail(_manager.Health["EURUSD"].OverallProgressHistory.First(x => x.Progress.Contains("Failed")).Progress);
                }

                await Task.Delay(50);
            }

            // Assert

            void CheckCandles(PriceType priceType, TimeInterval timeInterval, int expectedCount)
            {
                Assert.IsTrue(persistedCandleCounter.ContainsKey((timeInterval, priceType)), $"{timeInterval}, {priceType}");
                Assert.AreEqual(expectedCount, persistedCandleCounter[(timeInterval, priceType)], $"{timeInterval}, {priceType}");
            }

            foreach (var priceType in new[] { PriceType.Ask, PriceType.Bid, PriceType.Mid })
            {
                CheckCandles(priceType, TimeInterval.Sec, 60 * 60 * 24);
                CheckCandles(priceType, TimeInterval.Minute, 60 * 24);
                CheckCandles(priceType, TimeInterval.Hour, 24);
                CheckCandles(priceType, TimeInterval.Day, 1);
                CheckCandles(priceType, TimeInterval.Week, 1);
                CheckCandles(priceType, TimeInterval.Month, 1);
            }
        }
    }
}

