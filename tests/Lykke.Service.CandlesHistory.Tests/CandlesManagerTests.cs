using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices.Model;
using Lykke.Service.Assets.Client.Custom;
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
        private static readonly ImmutableArray<TimeInterval> StoredIntervals = ImmutableArray.Create
        (
            TimeInterval.Sec,
            TimeInterval.Minute,
            TimeInterval.Min30,
            TimeInterval.Hour,
            TimeInterval.Day,
            TimeInterval.Week,
            TimeInterval.Month
        );

        private List<IAssetPair> _assetPairs;

        private Mock<IMidPriceQuoteGenerator> _midPriceQuoteGeneratorMock;
        private Mock<ICandlesCacheService> _cacheServiceMock;
        private Mock<ICandlesHistoryRepository> _historyRepositoryMock;
        private Mock<IAssetPairsManager> _assetPairsManagerMock;
        private ICandlesManager _manager;
        
        private Mock<ICandlesPersistenceQueue> _persistenceQueueMock;
        private Mock<ICandlesGenerator> _candlesGeneratorMock;

        [TestInitialize]
        public void InitializeTest()
        {
            _midPriceQuoteGeneratorMock = new Mock<IMidPriceQuoteGenerator>();
            _candlesGeneratorMock = new Mock<ICandlesGenerator>();
            _cacheServiceMock = new Mock<ICandlesCacheService>();
            _historyRepositoryMock = new Mock<ICandlesHistoryRepository>();
            _assetPairsManagerMock = new Mock<IAssetPairsManager>();
            _persistenceQueueMock = new Mock<ICandlesPersistenceQueue>();

            _assetPairs = new List<IAssetPair>
            {
                new AssetPairResponseModel {Id = "EURUSD", Accuracy = 3},
                new AssetPairResponseModel {Id = "USDCHF", Accuracy = 2},
                new AssetPairResponseModel {Id = "EURRUB", Accuracy = 2}
            };

            _assetPairsManagerMock
                .Setup(m => m.GetAllEnabledAsync())
                .ReturnsAsync(() => _assetPairs);
            _assetPairsManagerMock
                .Setup(m => m.TryGetEnabledPairAsync(It.IsAny<string>()))
                .ReturnsAsync((string assetPairId) => _assetPairs.SingleOrDefault(a => a.Id == assetPairId));
            _historyRepositoryMock
                .Setup(m => m.CanStoreAssetPair(It.IsAny<string>()))
                .Returns((string assetPairId) => new[] {"EURUSD", "USDCHF", "USDRUB"}.Contains(assetPairId));
                
            _manager = new CandlesManager(
                _midPriceQuoteGeneratorMock.Object,
                _cacheServiceMock.Object,
                _historyRepositoryMock.Object,
                _assetPairsManagerMock.Object, 
                _persistenceQueueMock.Object,
                _candlesGeneratorMock.Object);
        }
        

        #region Quote processing

        [TestMethod]
        public async Task Only_quote_for_existing_enabled_asset_pairs_with_connection_string_are_processed()
        {
            // Arrange
            var quote1 = new Quote {AssetPair = "EURUSD"};
            var quote2 = new Quote {AssetPair = "USDRUB"};
            var quote3 = new Quote {AssetPair = "EURRUB"};

            // Act
            await _manager.ProcessQuoteAsync(quote1);
            await _manager.ProcessQuoteAsync(quote2);
            await _manager.ProcessQuoteAsync(quote3);

            // Assert
            foreach (var interval in StoredIntervals)
            {
                _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.Is<string>(p => p == "EURUSD"), It.Is<PriceType>(p => p == PriceType.Ask), It.Is<TimeInterval>(i => i == interval)), Times.Once);
                _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.Is<string>(p => p == "EURUSD"), It.Is<PriceType>(p => p == PriceType.Mid), It.Is<TimeInterval>(i => i == interval)), Times.Never);
                _persistenceQueueMock.Verify(s => s.EnqueueCandle(It.IsAny<IFeedCandle>(), It.Is<string>(a => a == "EURUSD"), It.IsAny<PriceType>(), It.Is<TimeInterval>(i => i == interval)), Times.Once);
            }

            _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.Is<string>(p => p == "USDRUB"), It.IsAny<PriceType>(), It.IsAny<TimeInterval>()), Times.Never, "No asset pair");
            _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.Is<string>(p => p == "EURRUB"), It.IsAny<PriceType>(), It.IsAny<TimeInterval>()), Times.Never, "No connection string");
            _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.Is<string>(p => p == "EURCHF"), It.IsAny<PriceType>(), It.IsAny<TimeInterval>()), Times.Never, "No asset pair nor connection string");

            _persistenceQueueMock.Verify(s => s.EnqueueCandle(It.IsAny<IFeedCandle>(), It.Is<string>(a => a == "USDRUB"), It.IsAny<PriceType>(), It.IsAny<TimeInterval>()), Times.Never, "No asset pair");
            _persistenceQueueMock.Verify(s => s.EnqueueCandle(It.IsAny<IFeedCandle>(), It.Is<string>(a => a == "EURRUB"), It.IsAny<PriceType>(), It.IsAny<TimeInterval>()), Times.Never, "No connection string");
            _persistenceQueueMock.Verify(s => s.EnqueueCandle(It.IsAny<IFeedCandle>(), It.Is<string>(a => a == "EURCHF"), It.IsAny<PriceType>(), It.IsAny<TimeInterval>()), Times.Never, "No asset pair nor connection string");
        }

        [TestMethod]
        public async Task Processing_buy_quote_produces_bid_price_type()
        {
            // Arrange
            var quote = new Quote {AssetPair = "EURUSD", IsBuy = true};

            // Act
            await _manager.ProcessQuoteAsync(quote);

            // Assert
            foreach (var interval in StoredIntervals)
            {
                _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p == PriceType.Bid), It.Is<TimeInterval>(i => i == interval)), Times.Once);
                _persistenceQueueMock.Verify(s => s.EnqueueCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p == PriceType.Bid), It.Is<TimeInterval>(i => i == interval)), Times.Once);
            }

            _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p != PriceType.Bid), It.IsAny<TimeInterval>()), Times.Never);
            _persistenceQueueMock.Verify(s => s.EnqueueCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p != PriceType.Bid), It.IsAny<TimeInterval>()), Times.Never);
        }

        [TestMethod]
        public async Task Processing_sell_quote_produces_ask_price_type()
        {
            // Arrange
            var quote = new Quote { AssetPair = "EURUSD", IsBuy = false };

            // Act
            await _manager.ProcessQuoteAsync(quote);

            // Assert
            foreach (var interval in StoredIntervals)
            {
                _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p == PriceType.Ask), It.Is<TimeInterval>(i => i == interval)), Times.Once);
                _persistenceQueueMock.Verify(s => s.EnqueueCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p == PriceType.Ask), It.Is<TimeInterval>(i => i == interval)), Times.Once);
            }

            _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p != PriceType.Ask), It.IsAny<TimeInterval>()), Times.Never);
            _persistenceQueueMock.Verify(s => s.EnqueueCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p != PriceType.Ask), It.IsAny<TimeInterval>()), Times.Never);
        }

        [TestMethod]
        public async Task If_quote_processing_produces_mid_price_quote_then_it_produces_mid_price_type_among_other()
        {
            // Arrange
            var quote = new Quote { AssetPair = "EURUSD", IsBuy = false };
            _midPriceQuoteGeneratorMock.Setup(g => g.TryGenerate(It.IsAny<IQuote>(), It.IsAny<int>())).Returns((IQuote q, int a) => q);

            // Act
            await _manager.ProcessQuoteAsync(quote);

            // Assert
            foreach (var interval in StoredIntervals)
            {
                _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p == PriceType.Ask), It.Is<TimeInterval>(i => i == interval)), Times.Once);
                _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p == PriceType.Mid), It.Is<TimeInterval>(i => i == interval)), Times.Once);

                _persistenceQueueMock.Verify(s => s.EnqueueCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p == PriceType.Ask), It.Is<TimeInterval>(i => i == interval)), Times.Once);
                _persistenceQueueMock.Verify(s => s.EnqueueCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p == PriceType.Mid), It.Is<TimeInterval>(i => i == interval)), Times.Once);
            }

            _cacheServiceMock.Verify(s => s.AddCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p == PriceType.Bid), It.IsAny<TimeInterval>()), Times.Never);
            _persistenceQueueMock.Verify(s => s.EnqueueCandle(It.IsAny<IFeedCandle>(), It.IsAny<string>(), It.Is<PriceType>(p => p == PriceType.Bid), It.IsAny<TimeInterval>()), Times.Never);
        }

        #endregion


        #region Candles getting

        [TestMethod]
        public async Task Getting_candles_of_asset_pair_that_hasnt_connection_string_throws()
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
                _manager.GetCandlesAsync("EURRUB", PriceType.Mid, TimeInterval.Minute, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23)));
        }

        [TestMethod]
        public async Task Getting_candles_of_asset_pair_that_doesnt_exists_throws()
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                _manager.GetCandlesAsync("USDRUB", PriceType.Mid, TimeInterval.Minute, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23)));
        }

        [TestMethod]
        public async Task Getting_candles_passes_asset_price_type_and_time_interval_to_cached_candles_service_and_repository()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Minute, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandles(
                    It.Is<string>(a => a == "EURUSD"),
                    It.Is<PriceType>(p => p == PriceType.Mid),
                    It.Is<TimeInterval>(i => i == TimeInterval.Minute),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);

            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
                    It.Is<string>(a => a == "EURUSD"),
                    It.Is<TimeInterval>(i => i == TimeInterval.Minute),
                    It.Is<PriceType>(p => p == PriceType.Mid),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Getting_candles_aligns_from_and_to_moments_according_to_time_interval()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", PriceType.Bid, TimeInterval.Day, new DateTime(2017, 06, 23, 17, 18, 00), new DateTime(2017, 07, 23, 17, 18, 00));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandles(
                    It.IsAny<string>(), It.IsAny<PriceType>(), It.IsAny<TimeInterval>(),
                    It.Is<DateTime>(d => d == new DateTime(2017, 06, 23)),
                    It.Is<DateTime>(d => d == new DateTime(2017, 07, 23))),
                Times.Once);

            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(), It.IsAny<TimeInterval>(), It.IsAny<PriceType>(),
                    It.Is<DateTime>(d => d == new DateTime(2017, 06, 23)),
                    It.Is<DateTime>(d => d == new DateTime(2017, 07, 23))),
                Times.Once);
        }

        [TestMethod]
        public async Task Getting_candles_with_time_interval_that_is_stored_doing_directly()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Minute, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandles(
                    It.IsAny<string>(), It.IsAny<PriceType>(),
                    It.Is<TimeInterval>(i => i == TimeInterval.Minute),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);

            _cacheServiceMock.Verify(s => s.GetCandles(
                    It.IsAny<string>(), It.IsAny<PriceType>(),
                    It.Is<TimeInterval>(i => i != TimeInterval.Minute),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);

            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(),
                    It.Is<TimeInterval>(i => i == TimeInterval.Minute),
                    It.IsAny<PriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);

            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(),
                    It.Is<TimeInterval>(i => i != TimeInterval.Minute),
                    It.IsAny<PriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Getting_candles_with_time_interval_that_is_not_stored_doing_by_mapping_from_nearest_smaller_stored_interval()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Min5, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Min15, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Hour4, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Hour6, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Hour12, new DateTime(2017, 06, 23, 17, 18, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandles(
                    It.IsAny<string>(), It.IsAny<PriceType>(),
                    It.Is<TimeInterval>(i => i == TimeInterval.Minute),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Exactly(2));

            _cacheServiceMock.Verify(s => s.GetCandles(
                    It.IsAny<string>(), It.IsAny<PriceType>(),
                    It.Is<TimeInterval>(i => i == TimeInterval.Hour),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Exactly(3));

            _cacheServiceMock.Verify(s => s.GetCandles(
                    It.IsAny<string>(), It.IsAny<PriceType>(),
                    It.Is<TimeInterval>(i => i != TimeInterval.Minute && i != TimeInterval.Hour),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);

            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(),
                    It.Is<TimeInterval>(i => i == TimeInterval.Minute),
                    It.IsAny<PriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Exactly(2));

            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(),
                    It.Is<TimeInterval>(i => i == TimeInterval.Hour),
                    It.IsAny<PriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Exactly(3));

            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(),
                    It.Is<TimeInterval>(i => i != TimeInterval.Minute && i != TimeInterval.Hour),
                    It.IsAny<PriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Getting_candles_with_time_interval_that_is_not_stored_doing_by_merging_candles_to_requested_interval()
        {
            // Arrange
            _cacheServiceMock
                .Setup(s => s.GetCandles(It.IsAny<string>(), It.IsAny<PriceType>(), It.IsAny<TimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                 .Returns((string a, PriceType p, TimeInterval i, DateTime f, DateTime t) => new IFeedCandle[]
                {
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 13, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 14, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 15, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 16, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 17, 00)},
                });

            // Act
            var candles = (await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Min15, new DateTime(2017, 06, 23), new DateTime(2017, 07, 23))).ToArray();

            // Assert
            Assert.AreEqual(2, candles.Length);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 00, 00), candles[0].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 15, 00), candles[1].DateTime);
        }

        [TestMethod]
        public async Task Getting_candles_that_covers_requested_fromMoment_are_gets_only_from_cache()
        {
            // Arrange
            _cacheServiceMock
                .Setup(s => s.GetCandles(It.IsAny<string>(), It.IsAny<PriceType>(), It.IsAny<TimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns((string a, PriceType p, TimeInterval i, DateTime f, DateTime t) => new IFeedCandle[]
                {
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 13, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 14, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 15, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 16, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 17, 00)},
                });

            // Act
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Minute, new DateTime(2017, 06, 23, 17, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandles(It.IsAny<string>(), It.IsAny<PriceType>(), It.IsAny<TimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);

            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<TimeInterval>(), It.IsAny<PriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Getting_candles_that_not_covers_requested_fromMoment_are_gets_from_cache_and_repository()
        {
            // Arrange
            _cacheServiceMock
                .Setup(s => s.GetCandles(It.IsAny<string>(), It.IsAny<PriceType>(), It.IsAny<TimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns((string a, PriceType p, TimeInterval i, DateTime f, DateTime t) => new IFeedCandle[]
                {
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 13, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 14, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 15, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 16, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 17, 00)},
                });

            // Act
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Minute, new DateTime(2017, 06, 23, 16, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandles(It.IsAny<string>(),  It.IsAny<PriceType>(), It.IsAny<TimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);

            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<TimeInterval>(), It.IsAny<PriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Getting_candles_that_not_cached_at_all_for_requested_from_and_to_moments_are_gets_from_cache_and_repository()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Minute, new DateTime(2017, 06, 23, 16, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _cacheServiceMock.Verify(s => s.GetCandles(It.IsAny<string>(), It.IsAny<PriceType>(), It.IsAny<TimeInterval>(), It.IsAny<DateTime>(),It.IsAny<DateTime>()),
                Times.Once);

            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(It.IsAny<string>(), It.IsAny<TimeInterval>(), It.IsAny<PriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Getting_candles_that_not_cached_at_all_for_requested_from_and_to_moments_passes_orignal_to_moment_to_repository()
        {
            // Act
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Sec, new DateTime(2017, 06, 23, 16, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(), It.IsAny<TimeInterval>(), It.IsAny<PriceType>(), It.IsAny<DateTime>(),
                    It.Is<DateTime>(d => d == new DateTime(2017, 07, 23, 17, 18, 23))),
                Times.Once);
        }

        [TestMethod]
        public async Task Getting_candles_that_not_covers_requested_fromMoment_passes_oldest_cached_candle_dateTime_as_to_moment_to_repository()
        {
            // Arrange
            _cacheServiceMock
                .Setup(s => s.GetCandles(It.IsAny<string>(), It.IsAny<PriceType>(), It.IsAny<TimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns((string a, PriceType p, TimeInterval i, DateTime f, DateTime t) => new IFeedCandle[]
                {
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 13, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 14, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 15, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 16, 00)},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 17, 00)},
                });

            // Act
            await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Minute, new DateTime(2017, 06, 23, 16, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23));

            // Assert
            _historyRepositoryMock.Verify(s => s.GetCandlesAsync(
                    It.IsAny<string>(), It.IsAny<TimeInterval>(), It.IsAny<PriceType>(), It.IsAny<DateTime>(),
                    It.Is<DateTime>(d => d == new DateTime(2017, 06, 23, 17, 13, 00))),
                Times.Once);
        }

        [TestMethod]
        public async Task Getting_candles_that_not_cached_at_all_for_requested_from_and_to_moments_returns_all_candles_returned_by_repository()
        {
            // Arrange
            _historyRepositoryMock
                .Setup(r => r.GetCandlesAsync(It.IsAny<string>(), It.IsAny<TimeInterval>(), It.IsAny<PriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((string a, TimeInterval i, PriceType p, DateTime f, DateTime t) => new IFeedCandle[]
                {
                    new FeedCandle {DateTime = new DateTime(2017, 06, 23, 17, 13, 00)},
                    new FeedCandle {DateTime = new DateTime(2017, 06, 23, 17, 14, 00)},
                    new FeedCandle {DateTime = new DateTime(2017, 06, 23, 17, 15, 00)},
                    new FeedCandle {DateTime = new DateTime(2017, 06, 23, 17, 16, 00)},
                    new FeedCandle {DateTime = new DateTime(2017, 06, 23, 17, 17, 00)},
                });

            // Act
            var candles = (await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Minute, new DateTime(2017, 06, 23, 17, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23))).ToArray();

            // Assert
            Assert.AreEqual(5, candles.Length);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 13, 00), candles[0].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 14, 00), candles[1].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 15, 00), candles[2].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 16, 00), candles[3].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 17, 00), candles[4].DateTime);
        }

        [TestMethod]
        public async Task Getting_candles_that_not_covers_requested_fromMoment_returns_candles_returned_by_repository_up_to_the_oldest_cached_candle_and_all_cached_candles()
        {
            // Arrange
            _historyRepositoryMock
                .Setup(r => r.GetCandlesAsync(It.IsAny<string>(), It.IsAny<TimeInterval>(), It.IsAny<PriceType>(),
                    It.Is<DateTime>(d => d == new DateTime(2017, 06, 23, 16, 13, 00)),
                    It.Is<DateTime>(d => d == new DateTime(2017, 06, 23, 17, 13, 00))))
                .ReturnsAsync((string a, TimeInterval i, PriceType p, DateTime f, DateTime t) => new IFeedCandle[]
                {
                    new FeedCandle {DateTime = new DateTime(2017, 06, 23, 17, 10, 00), Open = 1},
                    new FeedCandle {DateTime = new DateTime(2017, 06, 23, 17, 11, 00), Open = 1},
                    new FeedCandle {DateTime = new DateTime(2017, 06, 23, 17, 12, 00), Open = 1},
                    new FeedCandle {DateTime = new DateTime(2017, 06, 23, 17, 13, 00), Open = 1}
                });

            _cacheServiceMock
                .Setup(s => s.GetCandles(It.IsAny<string>(), It.IsAny<PriceType>(), It.IsAny<TimeInterval>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns((string a, PriceType p, TimeInterval i, DateTime f, DateTime t) => new IFeedCandle[]
                {
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 13, 00), Open = 2},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 14, 00), Open = 2},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 15, 00), Open = 2},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 16, 00), Open = 2},
                    new FeedCandle{DateTime = new DateTime(2017, 06, 23, 17, 17, 00), Open = 2},
                });

            // Act
            var candles = (await _manager.GetCandlesAsync("EURUSD", PriceType.Mid, TimeInterval.Minute, new DateTime(2017, 06, 23, 16, 13, 34), new DateTime(2017, 07, 23, 17, 18, 23))).ToArray();

            // Assert
            Assert.AreEqual(8, candles.Length);

            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 10, 00), candles[0].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 11, 00), candles[1].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 12, 00), candles[2].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 13, 00), candles[3].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 14, 00), candles[4].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 15, 00), candles[5].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 16, 00), candles[6].DateTime);
            Assert.AreEqual(new DateTime(2017, 06, 23, 17, 17, 00), candles[7].DateTime);

            Assert.AreEqual(1, candles[0].Open);
            Assert.AreEqual(1, candles[1].Open);
            Assert.AreEqual(1, candles[2].Open);
            Assert.AreEqual(2, candles[3].Open);
            Assert.AreEqual(2, candles[4].Open);
            Assert.AreEqual(2, candles[4].Open);
            Assert.AreEqual(2, candles[4].Open);
            Assert.AreEqual(2, candles[4].Open);
        }

        #endregion
    }
}