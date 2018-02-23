using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.CandlesProducer.Contract;
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
            _service = new InMemoryCandlesCacheService(AmountOfCandlesToStore, logMock.Object);
        }


        #region History initialization

        [TestMethod]
        public async Task History_initializes_and_then_obtains()
        {
            // Arrange
            var history = new[]
            {
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.5,
                    Close = 1.6,
                    Low = 1.3,
                    High = 1.8,
                    Timestamp = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6,
                    Close = 1.7,
                    Low = 1.4,
                    High = 1.9,
                    Timestamp = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6,
                    Close = 1.2,
                    Low = 1.3,
                    High = 1.5,
                    Timestamp = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.3,
                    Close = 1.6,
                    Low = 1.3,
                    High = 1.8,
                    Timestamp = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6,
                    Close = 1.6,
                    Low = 1.4,
                    High = 1.7,
                    Timestamp = new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc)
                },
            };

            // Act
            await _service.InitializeAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day, history);

            var obtainedHistory = (await _service.GetCandlesAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day,
                    new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc)))
                .ToArray();

            // Assert
            Assert.AreEqual(5, obtainedHistory.Length);
            Assert.AreEqual(new DateTime(2017, 06, 10), obtainedHistory[0].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 11), obtainedHistory[1].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 12), obtainedHistory[2].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 13), obtainedHistory[3].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 14), obtainedHistory[4].Timestamp);
        }

        [TestMethod]
        public async Task To_many_history_truncated_at_initialization()
        {
            // Arrange
            var history = new[]
            {
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.5,
                    Close = 1.6,
                    Low = 1.3,
                    High = 1.8,
                    Timestamp = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6,
                    Close = 1.7,
                    Low = 1.4,
                    High = 1.9,
                    Timestamp = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6,
                    Close = 1.2,
                    Low = 1.3,
                    High = 1.5,
                    Timestamp = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.3,
                    Close = 1.6,
                    Low = 1.3,
                    High = 1.8,
                    Timestamp = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6,
                    Close = 1.6,
                    Low = 1.4,
                    High = 1.7,
                    Timestamp = new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6,
                    Close = 1.6,
                    Low = 1.4,
                    High = 1.7,
                    Timestamp = new DateTime(2017, 06, 15, 0, 0, 0, DateTimeKind.Utc)
                },
            };

            // Act
            await _service.InitializeAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day, history);

            var obtainedHistory = (await _service.GetCandlesAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day,
                    new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc)))
                .ToArray();

            // Assert
            Assert.AreEqual(5, obtainedHistory.Length);
            Assert.AreEqual(new DateTime(2017, 06, 10), obtainedHistory[0].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 11), obtainedHistory[1].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 12), obtainedHistory[2].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 13), obtainedHistory[3].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 14), obtainedHistory[4].Timestamp);
        }

        [TestMethod]
        public async Task Not_full_history_initializes()
        {
            // Arrange
            var history = new[]
            {
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.5,
                    Close = 1.6,
                    Low = 1.3,
                    High = 1.8,
                    Timestamp = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6,
                    Close = 1.7,
                    Low = 1.4,
                    High = 1.9,
                    Timestamp = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6,
                    Close = 1.2,
                    Low = 1.3,
                    High = 1.5,
                    Timestamp = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.3,
                    Close = 1.6,
                    Low = 1.3,
                    High = 1.8,
                    Timestamp = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc)
                },
            };

            // Act
            await _service.InitializeAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day, history);

            var obtainedHistory = (await _service.GetCandlesAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day,
                    new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc)))
                .ToArray();

            // Assert
            Assert.AreEqual(4, obtainedHistory.Length);
            Assert.AreEqual(new DateTime(2017, 06, 10), obtainedHistory[0].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 11), obtainedHistory[1].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 12), obtainedHistory[2].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 13), obtainedHistory[3].Timestamp);
        }

        #endregion
        
        #region Candles getting

        [TestMethod]
        public async Task Getting_empty_history_returns_empty_enumerable()
        {
            // Act
            var candles = await _service.GetCandlesAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day,
                new DateTime(2017, 06, 01, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2017, 07, 01, 0, 0, 0, DateTimeKind.Utc));

            // Assert
            Assert.IsNotNull(candles);
            Assert.IsFalse(candles.Any());
        }

        [TestMethod]
        public async Task Getting_filtered_by_from_and_to_moments_candles_returns_suitable_candles()
        {
            // Arrange
            var history = new[]
            {
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8,
                    Timestamp = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9,
                    Timestamp = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5,
                    Timestamp = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8,
                    Timestamp = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7,
                    Timestamp = new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc)
                },
            };

            await _service.InitializeAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day, history);

            // Act
            var candles = (await _service.GetCandlesAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day,
                    new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc)))
                .ToArray();

            // Assert
            Assert.AreEqual(3, candles.Length);
            Assert.AreEqual(new DateTime(2017, 06, 11), candles[0].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 12), candles[1].Timestamp);
            Assert.AreEqual(new DateTime(2017, 06, 13), candles[2].Timestamp);
        }

        [TestMethod]
        public async Task Getting_filtered_by_from_and_to_moments_candles_returns_no_candles()
        {
            // Arrange
            var history = new[]
            {
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.5, Close = 1.6, Low = 1.3, High = 1.8,
                    Timestamp = new DateTime(2017, 06, 10, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6, Close = 1.7, Low = 1.4, High = 1.9,
                    Timestamp = new DateTime(2017, 06, 11, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6, Close = 1.2, Low = 1.3, High = 1.5,
                    Timestamp = new DateTime(2017, 06, 12, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.3, Close = 1.6, Low = 1.3, High = 1.8,
                    Timestamp = new DateTime(2017, 06, 13, 0, 0, 0, DateTimeKind.Utc)
                },
                new TestCandle
                {
                    AssetPairId = "EURUSD",
                    PriceType = CandlePriceType.Ask,
                    TimeInterval = CandleTimeInterval.Day,
                    Open = 1.6, Close = 1.6, Low = 1.4, High = 1.7,
                    Timestamp = new DateTime(2017, 06, 14, 0, 0, 0, DateTimeKind.Utc)
                },
            };

            await _service.InitializeAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day, history);

            // Act
            var candles1 = await _service.GetCandlesAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day, new DateTime(2017, 05, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 05, 13, 0, 0, 0, DateTimeKind.Utc));
            var candles2 = await _service.GetCandlesAsync("EURUSD", CandlePriceType.Ask, CandleTimeInterval.Day, new DateTime(2017, 07, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 07, 13, 0, 0, 0, DateTimeKind.Utc));

            // Assert
            Assert.IsFalse(candles1.Any());
            Assert.IsFalse(candles2.Any());
        }

        #endregion
    }
}
