using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
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
using IDateTimeProvider = Lykke.Service.CandlesHistory.Core.Services.IDateTimeProvider;

namespace Lykke.Service.CandlesHistory.Tests
{
    public class CandlesCacheInitializationTest
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

        private static readonly ImmutableArray<PriceType> StoredPriceTypes = ImmutableArray.Create
        (
            PriceType.Ask,
            PriceType.Bid,
            PriceType.Mid
        );

        private const int AmountOfCandlesToStore = 5;

        private ICandlesCacheInitalizationService _service;
        private Mock<IDateTimeProvider> _dateTimeProviderMock;
        private Mock<IMidPriceQuoteGenerator> _midPriceQuoteGeneratorMock;
        private Mock<ICandlesCacheService> _cacheServiceMock;
        private Mock<ICandlesHistoryRepository> _historyRepositoryMock;
        private Mock<IAssetPairsManager> _assetPairsManagerMock;
        private List<IAssetPair> _assetPairs;

        [TestInitialize]
        public void InitializeTest()
        {
            var logMock = new Mock<ILog>();

            _dateTimeProviderMock = new Mock<IDateTimeProvider>();
            _midPriceQuoteGeneratorMock = new Mock<IMidPriceQuoteGenerator>();
            _cacheServiceMock = new Mock<ICandlesCacheService>();
            _historyRepositoryMock = new Mock<ICandlesHistoryRepository>();
            _assetPairsManagerMock = new Mock<IAssetPairsManager>();

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

            _service = new CandlesCacheInitalizationService(
                logMock.Object,
                _assetPairsManagerMock.Object,
                _dateTimeProviderMock.Object,
                _midPriceQuoteGeneratorMock.Object,
                _cacheServiceMock.Object,
                _historyRepositoryMock.Object,
                AmountOfCandlesToStore);
        }

        [TestMethod]
        public async Task Initialization_caches_each_asset_pairs_in_each_stored_interval_and_in_each_stored_price_type_from_persistent_storage()
        {
            // Arrange
            var now = new DateTime(2017, 06, 23, 15, 35, 20, DateTimeKind.Utc);

            _dateTimeProviderMock.SetupGet(p => p.UtcNow).Returns(now);
            _historyRepositoryMock
                .Setup(r => r.GetCandlesAsync(It.IsAny<string>(), It.IsAny<TimeInterval>(), It.IsAny<PriceType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((string a, TimeInterval i, PriceType p, DateTime f, DateTime t) => new[] { new FeedCandle(), new FeedCandle() });

            // Act
            await _service.InitializeCacheAsync();

            // Assert
            foreach (var interval in StoredIntervals)
            {
                foreach (var priceType in StoredPriceTypes)
                {
                    foreach (var assetPairId in new[] { "EURUSD", "USDCHF" })
                    {
                        _historyRepositoryMock.Verify(r =>
                                r.GetCandlesAsync(
                                    It.Is<string>(a => a == assetPairId),
                                    It.Is<TimeInterval>(i => i == interval),
                                    It.Is<PriceType>(p => p == priceType),
                                    It.Is<DateTime>(d => d == now.RoundTo(interval).AddIntervalTicks(-AmountOfCandlesToStore, interval)),
                                    It.Is<DateTime>(d => d == now.RoundTo(interval))),
                            Times.Once);

                        _cacheServiceMock.Verify(s =>
                                s.Initialize(
                                    It.Is<string>(a => a == assetPairId),
                                    It.Is<TimeInterval>(i => i == interval),
                                    It.Is<PriceType>(p => p == priceType),
                                    It.Is<IEnumerable<IFeedCandle>>(c => c.Count() == 2)),
                            Times.Once);
                    }
                }
            }

            _historyRepositoryMock.Verify(r =>
                    r.GetCandlesAsync(
                        It.Is<string>(a => !new[] { "EURUSD", "USDCHF" }.Contains(a)),
                        It.IsAny<TimeInterval>(),
                        It.IsAny<PriceType>(),
                        It.IsAny<DateTime>(),
                        It.IsAny<DateTime>()),
                Times.Never);

            _cacheServiceMock.Verify(s =>
                    s.Initialize(
                        It.Is<string>(a => !new[] { "EURUSD", "USDCHF" }.Contains(a)),
                        It.IsAny<TimeInterval>(),
                        It.IsAny<PriceType>(),
                        It.IsAny<IEnumerable<IFeedCandle>>()),
                Times.Never);
        }
    }
}