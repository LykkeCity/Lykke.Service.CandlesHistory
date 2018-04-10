﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Lykke.Service.CandlesHistory.Tests
{
    public class CandlesCacheInitializationTest
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

        // ReSharper disable once UnusedMember.Local
        private static readonly ImmutableArray<CandlePriceType> StoredPriceTypes = ImmutableArray.Create
        (
            CandlePriceType.Ask,
            CandlePriceType.Bid,
            CandlePriceType.Mid,
            CandlePriceType.Trades
        );

        private Mock<IAssetPairsManager> _assetPairsManagerMock;
        private List<AssetPair> _assetPairs;

        [TestInitialize]
        public void InitializeTest()
        {
            _assetPairsManagerMock = new Mock<IAssetPairsManager>();

            _assetPairs = new List<AssetPair>
            {
                new AssetPair("EURUSD", 3),
                new AssetPair("USDCHF", 2),
                new AssetPair("EURRUB", 2)
            };

            _assetPairsManagerMock
                .Setup(m => m.GetAllEnabledAsync())
                .ReturnsAsync(() => _assetPairs);
            _assetPairsManagerMock
                .Setup(m => m.TryGetEnabledPairAsync(It.IsAny<string>()))
                .ReturnsAsync((string assetPairId) => _assetPairs.SingleOrDefault(a => a.Id == assetPairId));
        }
    }
}
