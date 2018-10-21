using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Candles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Lykke.Service.CandlesHistory.Tests
{
    [TestClass]
    public class CandlesManagerTests
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly ImmutableArray<CandleTimeInterval> StoredIntervals = ImmutableArray.Create
        (
            CandleTimeInterval.Sec,
            CandleTimeInterval.Minute,
            CandleTimeInterval.Min30,
            CandleTimeInterval.Hour,
            CandleTimeInterval.Day,
            CandleTimeInterval.Week,
            CandleTimeInterval.Month
        );

        private List<AssetPair> _assetPairs;

        private Mock<ICandlesCacheService> _cacheServiceMock;
        private Mock<ICandlesHistoryRepository> _historyRepositoryMock;
        private Mock<IAssetPairsManager> _assetPairsManagerMock;
        private ICandlesManager _manager;

        [TestInitialize]
        public void InitializeTest()
        {
            _cacheServiceMock = new Mock<ICandlesCacheService>();
            _historyRepositoryMock = new Mock<ICandlesHistoryRepository>();
            _assetPairsManagerMock = new Mock<IAssetPairsManager>();

            _assetPairs = new List<AssetPair>
            {
                new AssetPair {Id = "EURUSD", Accuracy = 3},
                new AssetPair {Id = "USDCHF", Accuracy = 2},
                new AssetPair {Id = "EURRUB", Accuracy = 2}
            };

            _assetPairsManagerMock
                .Setup(m => m.GetAllEnabledAsync())
                .ReturnsAsync(() => _assetPairs);
            _assetPairsManagerMock
                .Setup(m => m.TryGetEnabledPairAsync(It.IsAny<string>()))
                .ReturnsAsync((string assetPairId) => _assetPairs.SingleOrDefault(a => a.Id == assetPairId));

            _manager = new CandlesManager(
                _cacheServiceMock.Object,
                _historyRepositoryMock.Object);
        }

        #region Candles getting

        [TestMethod]
        public async Task Getting_candles_passes_asset_price_type_and_time_interval_to_cached_candles_service_and_repository()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Minute, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandlesAsync(
                    It.Is<string>(a => a == "EURUSD"),
                    It.Is<CandlePriceType>(p => p == CandlePriceType.Mid),
                    It.Is<CandleTimeInterval>(i => i == CandleTimeInterval.Minute),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);

//            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
//                    It.Is<string>(a => a == "EURUSD"),
//                    It.Is<CandleTimeInterval>(i => i == CandleTimeInterval.Minute),
//                    It.Is<CandlePriceType>(p => p == CandlePriceType.Mid),
//                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
//                Times.Once);
        }

        [TestMethod]
        public async Task Getting_candles_aligns_from_and_to_moments_according_to_time_interval()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Bid, CandleTimeInterval.Day, new DateTime(2017, 06, 23, 17, 18, 00), new DateTime(2017, 07, 23, 17, 18, 00));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(), It.IsAny<CandlePriceType>(), It.IsAny<CandleTimeInterval>(),
                    It.Is<DateTime>(d => d == new DateTime(2017, 06, 23)),
                    It.Is<DateTime>(d => d == new DateTime(2017, 07, 23))),
                Times.Once);

//            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
//                    It.IsAny<string>(), It.IsAny<CandleTimeInterval>(), It.IsAny<CandlePriceType>(),
//                    It.Is<DateTime>(d => d == new DateTime(2017, 06, 23)),
//                    It.Is<DateTime>(d => d == new DateTime(2017, 07, 23))),
//                Times.Once);
        }

        [TestMethod]
        public async Task Getting_candles_with_time_interval_that_is_stored_doing_directly()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Minute, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(), It.IsAny<CandlePriceType>(),
                    It.Is<CandleTimeInterval>(i => i == CandleTimeInterval.Minute),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);

            _cacheServiceMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(), It.IsAny<CandlePriceType>(),
                    It.Is<CandleTimeInterval>(i => i != CandleTimeInterval.Minute),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);

//            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
//                    It.IsAny<string>(),
//                    It.Is<CandleTimeInterval>(i => i == CandleTimeInterval.Minute),
//                    It.IsAny<CandlePriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
//                Times.Once);
//
//            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
//                    It.IsAny<string>(),
//                    It.Is<CandleTimeInterval>(i => i != CandleTimeInterval.Minute),
//                    It.IsAny<CandlePriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
//                Times.Never);
        }

        [TestMethod]
        public async Task Getting_candles_with_time_interval_that_is_not_stored_doing_by_mapping_from_nearest_smaller_stored_interval()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Min5, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Min15, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Hour4, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Hour6, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Hour12, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(), It.IsAny<CandlePriceType>(),
                    It.Is<CandleTimeInterval>(i => i == CandleTimeInterval.Minute),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Exactly(2));

            _cacheServiceMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(), It.IsAny<CandlePriceType>(),
                    It.Is<CandleTimeInterval>(i => i == CandleTimeInterval.Hour),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Exactly(3));

            _cacheServiceMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(), It.IsAny<CandlePriceType>(),
                    It.Is<CandleTimeInterval>(i => i != CandleTimeInterval.Minute && i != CandleTimeInterval.Hour),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);

//            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
//                    It.IsAny<string>(),
//                    It.Is<CandleTimeInterval>(i => i == CandleTimeInterval.Minute),
//                    It.IsAny<CandlePriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
//                Times.Exactly(2));
//
//            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
//                    It.IsAny<string>(),
//                    It.Is<CandleTimeInterval>(i => i == CandleTimeInterval.Hour),
//                    It.IsAny<CandlePriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
//                Times.Exactly(3));
//
//            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
//                    It.IsAny<string>(),
//                    It.Is<CandleTimeInterval>(i => i != CandleTimeInterval.Minute && i != CandleTimeInterval.Hour),
//                    It.IsAny<CandlePriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
//                Times.Never);
        }

        [TestMethod]
        public async Task Getting_candles_with_time_interval_that_is_not_stored_doing_by_merging_candles_to_requested_interval()
        {
            // Arrange
            _cacheServiceMock
                .Setup(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandlePriceType>(), It.IsAny<CandleTimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                 .Returns((string a, CandlePriceType p, CandleTimeInterval i, DateTime f, DateTime t) => Task.FromResult<IEnumerable<ICandle>>(new ICandle[]
                {
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 13, 00), TimeInterval = CandleTimeInterval.Min15, PriceType = CandlePriceType.Mid},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 14, 00), TimeInterval = CandleTimeInterval.Min15, PriceType = CandlePriceType.Mid},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 15, 00), TimeInterval = CandleTimeInterval.Min15, PriceType = CandlePriceType.Mid},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 16, 00), TimeInterval = CandleTimeInterval.Min15, PriceType = CandlePriceType.Mid},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 17, 00), TimeInterval = CandleTimeInterval.Min15, PriceType = CandlePriceType.Mid},
                }));

            // Act
            var candles = (await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Min15, new DateTime(2017, 06, 23), new DateTime(2017, 07, 23))).ToArray();

            // Assert
            Assert.AreEqual(2, candles.Length);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 00, 00), candles[0].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 15, 00), candles[1].Timestamp);
        }

        [TestMethod]
        public async Task Getting_candles_that_covers_requested_fromMoment_are_gets_only_from_cache()
        {
            // Arrange
            _cacheServiceMock
                .Setup(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandlePriceType>(), It.IsAny<CandleTimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns((string a, CandlePriceType p, CandleTimeInterval i, DateTime f, DateTime t) => Task.FromResult<IEnumerable<ICandle>>(new ICandle[]
                {
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 13, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 14, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 15, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 16, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 17, 00)},
                }));

            // Act
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Minute, new DateTime(2017, 06, 23, 17, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandlePriceType>(), It.IsAny<CandleTimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);

//            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandleTimeInterval>(), It.IsAny<CandlePriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
//                Times.Never);
        }

        [TestMethod]
        public async Task Getting_candles_that_not_covers_requested_fromMoment_are_gets_from_cache_and_repository()
        {
            // Arrange
            _cacheServiceMock
                .Setup(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandlePriceType>(), It.IsAny<CandleTimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns((string a, CandlePriceType p, CandleTimeInterval i, DateTime f, DateTime t) => Task.FromResult<IEnumerable<ICandle>>(new ICandle[]
                {
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 13, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 14, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 15, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 16, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 17, 00)},
                }));

            // Act
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Minute, new DateTime(2017, 06, 23, 16, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandlePriceType>(), It.IsAny<CandleTimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);

//            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandleTimeInterval>(), It.IsAny<CandlePriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
//                Times.Once);
        }

        [TestMethod]
        public async Task Getting_candles_that_not_cached_at_all_for_requested_from_and_to_moments_are_gets_from_cache_and_repository()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Minute, new DateTime(2017, 06, 23, 16, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandlePriceType>(), It.IsAny<CandleTimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);

//            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandleTimeInterval>(), It.IsAny<CandlePriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
//                Times.Once);
        }

        [TestMethod]
        public async Task Getting_candles_that_not_cached_at_all_for_requested_from_and_to_moments_passes_orignal_to_moment_to_repository()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Sec, new DateTime(2017, 06, 23, 16, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            
            _cacheServiceMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(), It.IsAny<CandlePriceType>(), It.IsAny<CandleTimeInterval>(), It.IsAny<DateTime>(),
                    It.Is<DateTime>(d => d == new DateTime(2017, 07, 23, 17, 18, 23))),
                Times.Once);
            
//            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
//                    It.IsAny<string>(), It.IsAny<CandleTimeInterval>(), It.IsAny<CandlePriceType>(), It.IsAny<DateTime>(),
//                    It.Is<DateTime>(d => d == new DateTime(2017, 07, 23, 17, 18, 23))),
//                Times.Once);
        }

        [TestMethod]
        [Ignore]
        public async Task Getting_candles_that_not_covers_requested_fromMoment_passes_oldest_cached_candle_dateTime_as_to_moment_to_repository()
        {
            // Arrange
            _cacheServiceMock
                .Setup(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandlePriceType>(), It.IsAny<CandleTimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns((string a, CandlePriceType p, CandleTimeInterval i, DateTime f, DateTime t) => Task.FromResult<IEnumerable<ICandle>>(new ICandle[]
                {
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 13, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 14, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 15, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 16, 00)},
                    new TestCandle{Timestamp = new DateTime(2017, 06, 23, 17, 17, 00)},
                }));

            // Act
            await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Minute, new DateTime(2017, 06, 23, 16, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            
            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(), It.IsAny<CandleTimeInterval>(), It.IsAny<CandlePriceType>(), It.IsAny<DateTime>(),
                    It.Is<DateTime>(d => d == new DateTime(2017, 06, 23, 17, 13, 00))),
                Times.Once);
        }

        [TestMethod]
        public async Task Getting_candles_that_not_cached_at_all_for_requested_from_and_to_moments_returns_all_candles_returned_by_repository()
        {
            // Arrange
//            _historyRepositoryMock
//                .Setup(r => r.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandleTimeInterval>(), It.IsAny<CandlePriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
//                .ReturnsAsync((string a, CandleTimeInterval i, CandlePriceType p, DateTime f, DateTime t) => new ICandle[]
//                {
//                    new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 13, 00)},
//                    new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 14, 00)},
//                    new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 15, 00)},
//                    new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 16, 00)},
//                    new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 17, 00)},
//                });

            _cacheServiceMock
                .Setup(r => r.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandlePriceType>(),
                    It.IsAny<CandleTimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((string a, CandleTimeInterval i, CandlePriceType p, DateTime f, DateTime t) =>
                    new ICandle[]
                    {
                        new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 13, 00)},
                        new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 14, 00)},
                        new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 15, 00)},
                        new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 16, 00)},
                        new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 17, 00)},
                    });

            // Act
            var candles = (await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Minute, new DateTime(2017, 06, 23, 17, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23))).ToArray();

            // Assert
            Assert.AreEqual(5, candles.Length);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 13, 00), candles[0].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 14, 00), candles[1].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 15, 00), candles[2].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 16, 00), candles[3].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 17, 00), candles[4].Timestamp);
        }

        [TestMethod]
        public async Task
            Getting_candles_that_not_covers_requested_fromMoment_returns_candles_returned_by_repository_up_to_the_oldest_cached_candle_and_all_cached_candles()
        {
            // Arrange
//            _historyRepositoryMock
//                .Setup(r => r.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandleTimeInterval>(), It.IsAny<CandlePriceType>(),
//                    It.Is<DateTime>(d => d == new DateTime(2017, 06, 23, 16, 13, 00)),
//                    It.Is<DateTime>(d => d == new DateTime(2017, 06, 23, 17, 13, 00))))
//                .ReturnsAsync((string a, CandleTimeInterval i, CandlePriceType p, DateTime f, DateTime t) => new ICandle[]
//                {
//                    new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 10, 00), Open = 1},
//                    new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 11, 00), Open = 1},
//                    new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 12, 00), Open = 1},
//                    new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 13, 00), Open = 1}
//                });

            _cacheServiceMock
                .Setup(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<CandlePriceType>(),
                    It.IsAny<CandleTimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns((string a, CandlePriceType p, CandleTimeInterval i, DateTime f, DateTime t) =>
                    Task.FromResult<IEnumerable<ICandle>>(new ICandle[]
                    {
                        new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 13, 00), Open = 2},
                        new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 14, 00), Open = 2},
                        new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 15, 00), Open = 2},
                        new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 16, 00), Open = 2},
                        new TestCandle {Timestamp = new DateTime(2017, 06, 23, 17, 17, 00), Open = 2},
                    }));

            // Act
            var candles = (await _manager.GetCandlesAsync("EURUSD", CandlePriceType.Mid, CandleTimeInterval.Minute,
                new DateTime(2017, 06, 23, 16, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23))).ToArray();

            // Assert
            //Assert.AreEqual(8, candles.Length);
            Assert.AreEqual(5, candles.Length);

//            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 10, 00), candles[0].Timestamp);
//            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 11, 00), candles[1].Timestamp);
//            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 12, 00), candles[2].Timestamp);
//            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 13, 00), candles[3].Timestamp);
//            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 14, 00), candles[4].Timestamp);
//            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 15, 00), candles[5].Timestamp);
//            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 16, 00), candles[6].Timestamp);
//            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 17, 00), candles[7].Timestamp);

            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 13, 00), candles[0].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 14, 00), candles[1].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 15, 00), candles[2].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 16, 00), candles[3].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 17, 00), candles[4].Timestamp);

//            Assert.AreEqual(1, candles[0].Open);
//            Assert.AreEqual(1, candles[1].Open);
//            Assert.AreEqual(1, candles[2].Open);
//            Assert.AreEqual(2, candles[3].Open);
//            Assert.AreEqual(2, candles[4].Open);
//            Assert.AreEqual(2, candles[4].Open);
//            Assert.AreEqual(2, candles[4].Open);
//            Assert.AreEqual(2, candles[4].Open);

            Assert.AreEqual(2, candles[0].Open);
            Assert.AreEqual(2, candles[1].Open);
            Assert.AreEqual(2, candles[2].Open);
            Assert.AreEqual(2, candles[3].Open);
            Assert.AreEqual(2, candles[4].Open);
        }

        #endregion
    }
}
