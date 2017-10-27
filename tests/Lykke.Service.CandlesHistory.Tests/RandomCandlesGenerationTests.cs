﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
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
            var bidAskHistory = new Dictionary<DateTime, FeedBidAskHistory>();

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

            _migrationServiceMock
                .Setup(x => x.SaveBidAskHistoryAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<ICandle>>(),
                    It.IsAny<PriceType>()))
                .Returns(
                    new Func<string, IEnumerable<ICandle>, PriceType, Task>((assetPair, candles, priceType) =>
                    {
                        var entities = candles
                            .GroupBy(c => c.Timestamp.RoundToMinute())
                            .Select(g => new FeedBidAskHistory
                            {
                                AssetPair = assetPair,
                                DateTime = g.Key,
                                AskCandles = priceType == PriceType.Ask ? g.ToArray() : null,
                                BidCandles = priceType == PriceType.Bid ? g.ToArray() : null
                            });

                        foreach (var entity in entities)
                        {
                            if (!bidAskHistory.TryGetValue(entity.DateTime, out var existingEntity))
                            {
                                bidAskHistory.Add(entity.DateTime, entity);
                            }
                            else
                            {
                                existingEntity.AskCandles = entity.AskCandles ?? existingEntity.AskCandles;
                                existingEntity.BidCandles = entity.BidCandles ?? existingEntity.BidCandles;
                            }
                        }

                        return Task.CompletedTask;
                    }));

            _migrationServiceMock
                .Setup(x => x.GetFeedHistoryBidAskByChunkAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsNotNull<Func<IEnumerable<IFeedBidAskHistory>, Task>>()))
                .Returns(new Func<string, DateTime, DateTime, Func<IEnumerable<IFeedBidAskHistory>, Task>, Task>(
                    (assetPair, startDate, endDate, chunkProcessor) =>
                    {
                        return chunkProcessor(bidAskHistory.Values);
                    }));

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
                await Task.Delay(50);
            }

            // Assert

            void CheckCandles(PriceType priceType, TimeInterval timeInterval, int expectedCount)
            {
                Assert.IsTrue(persistedCandleCounter.ContainsKey((timeInterval, priceType)), $"{timeInterval}, {priceType}");
                Assert.AreEqual(expectedCount, persistedCandleCounter[(timeInterval, priceType)], $"{timeInterval}, {priceType}");
            }

            foreach (var priceType in new[] { PriceType.Ask, PriceType.Bid })
            {
                CheckCandles(priceType, TimeInterval.Sec, 60 * 60 * 24);
                CheckCandles(priceType, TimeInterval.Minute, 60 * 24);
                CheckCandles(priceType, TimeInterval.Hour, 24);
                CheckCandles(priceType, TimeInterval.Day, 1);
                CheckCandles(priceType, TimeInterval.Week, 1);
                CheckCandles(priceType, TimeInterval.Month, 1);
            }

            // For mid candles, every candle enqued so many times, 
            // how many minutes is generated, since BidAskFeedHistory is grouped by minutes

            CheckCandles(PriceType.Mid, TimeInterval.Sec, 60 * 60 * 24);
            CheckCandles(PriceType.Mid, TimeInterval.Minute, 60 * 24);
            CheckCandles(PriceType.Mid, TimeInterval.Hour, 60 * 24);
            CheckCandles(PriceType.Mid, TimeInterval.Day, 60 * 24);
            CheckCandles(PriceType.Mid, TimeInterval.Week, 60 * 24);
            CheckCandles(PriceType.Mid, TimeInterval.Month, 60 * 24);
        }
    }
}
