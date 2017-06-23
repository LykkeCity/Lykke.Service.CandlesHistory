using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Services.Assets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Lykke.Service.CandlesHistory.Tests
{
    [TestClass]
    public class AssetPairsManagerTests
    {
        private readonly TimeSpan _cacheExpirationPeriod = TimeSpan.FromMinutes(1);

        private IAssetPairsManager _manager;
        private Mock<IAssetPairsRepository> _assetPairsRepositoryMock;
        private Mock<IAssetPairsCacheService> _assetPairsCacheServiceMock;
        private Mock<IDateTimeProvider> _dateTimeProviderMock;

        [TestInitialize]
        public void InitializeTest()
        {
            _assetPairsRepositoryMock = new Mock<IAssetPairsRepository>();
            _assetPairsCacheServiceMock = new Mock<IAssetPairsCacheService>();
            _dateTimeProviderMock = new Mock<IDateTimeProvider>();

            _manager = new AssetPairsManager(
                _assetPairsRepositoryMock.Object,
                _assetPairsCacheServiceMock.Object,
                _dateTimeProviderMock.Object,
                _cacheExpirationPeriod);
        }

        #region Getting enabled pair

        [TestMethod]
        public async Task Getting_enabled_pair_returns_enabled_pair()
        {
            // Arrange
            _assetPairsCacheServiceMock
                .Setup(s => s.TryGetPair(It.Is<string>(a => a == "EURUSD")))
                .Returns((string a) => new AssetPair { Id = a, IsDisabled = false });

            // Act
            var pair = await _manager.TryGetEnabledPairAsync("EURUSD");

            // Assert
            Assert.IsNotNull(pair);
            Assert.AreEqual("EURUSD", pair.Id);
        }

        [TestMethod]
        public async Task Getting_enabled_pair_not_returns_disabled_pair()
        {
            // Arrange
            _assetPairsCacheServiceMock
                .Setup(s => s.TryGetPair(It.Is<string>(a => a == "EURUSD")))
                .Returns((string a) => new AssetPair { Id = a, IsDisabled = true });

            // Act
            var pair = await _manager.TryGetEnabledPairAsync("EURUSD");

            // Assert
            Assert.IsNull(pair);
        }

        [TestMethod]
        public async Task Getting_enabled_pair_not_returns_missing_pair()
        {
            // Arrange
            _assetPairsCacheServiceMock
                .Setup(s => s.TryGetPair(It.Is<string>(a => a == "EURUSD")))
                .Returns((string a) => null);

            // Act
            var pair = await _manager.TryGetEnabledPairAsync("EURUSD");

            // Assert
            Assert.IsNull(pair);
        }

        #endregion


        #region Getting all enabled pairs

        [TestMethod]
        public async Task Getting_all_pairs_returns_empty_enumerable_if_no_enabled_pairs()
        {
            // Arrange
            _assetPairsCacheServiceMock
                .Setup(s => s.GetAll())
                .Returns(() => new[]
                {
                    new AssetPair { Id = "USDRUB", IsDisabled = true },
                });

            // Act
            var pairs = await _manager.GetAllEnabledAsync();

            // Assert
            Assert.IsNotNull(pairs);
            Assert.IsFalse(pairs.Any());
        }

        [TestMethod]
        public async Task Getting_all_pairs_returns_only_enabled_pairs()
        {
            // Arrange
            _assetPairsCacheServiceMock
                .Setup(s => s.GetAll())
                .Returns(() => new[]
                {
                    new AssetPair { Id = "EURUSD", IsDisabled = false },
                    new AssetPair { Id = "USDRUB", IsDisabled = true },
                    new AssetPair { Id = "USDCHF", IsDisabled = false }
                });

            // Act
            var pairs = (await _manager.GetAllEnabledAsync()).ToArray();

            // Assert
            Assert.AreEqual(2, pairs.Length);
            Assert.AreEqual("EURUSD", pairs[0].Id);
            Assert.AreEqual("USDCHF", pairs[1].Id);
        }

        #endregion


        #region Cache updating

        [TestMethod]
        public async Task Getting_pair_first_time_updates_cache()
        {
            // Arrange
            _dateTimeProviderMock.SetupGet(p => p.UtcNow).Returns(() => new DateTime(2017, 06, 23, 21, 00, 00));
            _assetPairsRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(() => new IAssetPair[]
                {
                    new AssetPair {Id = "EURUSD"},
                });

            // Act
            await _manager.TryGetEnabledPairAsync("EURUSD");

            // Asert
            _assetPairsRepositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
            // ReSharper disable once PossibleMultipleEnumeration
            _assetPairsCacheServiceMock.Verify(s => s.Update(It.Is<IEnumerable<IAssetPair>>(p => p.Count() == 1 && p.Count(a => a.Id == "EURUSD") == 1)), Times.Once);
        }

        [TestMethod]
        public async Task Getting_all_pairs_first_time_updates_cache()
        {
            // Arrange
            _dateTimeProviderMock.SetupGet(p => p.UtcNow).Returns(() => new DateTime(2017, 06, 23, 21, 00, 00));
            _assetPairsRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(() => new IAssetPair[]
                {
                    new AssetPair {Id = "EURUSD"},
                });
            _assetPairsCacheServiceMock.Setup(s => s.GetAll()).Returns(() => new IAssetPair[0]);

            // Act
            await _manager.GetAllEnabledAsync();

            // Asert
            _assetPairsRepositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
            // ReSharper disable once PossibleMultipleEnumeration
            _assetPairsCacheServiceMock.Verify(s => s.Update(It.Is<IEnumerable<IAssetPair>>(p => p.Count() == 1 && p.Count(a => a.Id == "EURUSD") == 1)), Times.Once);
        }

        [TestMethod]
        public async Task Getting_pair_once_after_cache_expiration_updates_cache()
        {
            // Arrange
            var initialNow = new DateTime(2017, 06, 23, 21, 00, 00);

            _dateTimeProviderMock.SetupGet(p => p.UtcNow).Returns(() => initialNow);
            _assetPairsRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(() => new IAssetPair[]
                {
                    new AssetPair {Id = "EURUSD"},
                });

            await _manager.TryGetEnabledPairAsync("EURUSD");

            _dateTimeProviderMock.SetupGet(p => p.UtcNow).Returns(() => initialNow.Add(_cacheExpirationPeriod).AddTicks(1));

            _assetPairsRepositoryMock.ResetCalls();
            _assetPairsCacheServiceMock.ResetCalls();
            
            // Act
            await _manager.TryGetEnabledPairAsync("EURUSD");

            // Asert
            _assetPairsRepositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
            // ReSharper disable once PossibleMultipleEnumeration
            _assetPairsCacheServiceMock.Verify(s => s.Update(It.Is<IEnumerable<IAssetPair>>(p => p.Count() == 1 && p.Count(a => a.Id == "EURUSD") == 1)), Times.Once);
        }

        [TestMethod]
        public async Task Getting_all_pairs_once_after_cache_expiration_updates_cache()
        {
            // Arrange
            var initialNow = new DateTime(2017, 06, 23, 21, 00, 00);

            _dateTimeProviderMock.SetupGet(p => p.UtcNow).Returns(() => initialNow);
            _assetPairsRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(() => new IAssetPair[]
                {
                    new AssetPair {Id = "EURUSD"},
                });
            _assetPairsCacheServiceMock.Setup(s => s.GetAll()).Returns(() => new IAssetPair[0]);

            await _manager.TryGetEnabledPairAsync("EURUSD");

            _dateTimeProviderMock.SetupGet(p => p.UtcNow).Returns(() => initialNow.Add(_cacheExpirationPeriod).AddTicks(1));

            _assetPairsRepositoryMock.ResetCalls();
            _assetPairsCacheServiceMock.ResetCalls();

            // Act
            await _manager.GetAllEnabledAsync();

            // Asert
            _assetPairsRepositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
            // ReSharper disable once PossibleMultipleEnumeration
            _assetPairsCacheServiceMock.Verify(s => s.Update(It.Is<IEnumerable<IAssetPair>>(p => p.Count() == 1 && p.Count(a => a.Id == "EURUSD") == 1)), Times.Once);
        }

        #endregion
    }
}