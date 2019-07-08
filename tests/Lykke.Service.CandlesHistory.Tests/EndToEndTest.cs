// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.CandlesHistory.Client.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.Service.CandlesHistory.Tests
{
    [TestClass, Ignore("Only for manual run")]
    public class EndToEndTest
    {
        private Candleshistoryservice _client;

        [TestInitialize]
        public void InitializeTest()
        {
            _client = new Candleshistoryservice(new Uri(@"http://localhost:5000"));
        }

        [TestMethod]
        public async Task ShouldSelectMonthlyCandlesForOneYear()
        {
            var pairs = await _client.GetAvailableAssetPairsAsync();

            for (int index = 0; index < 10; index++)
            {
                Parallel.For(0, 1000, (l, state) =>
                {
                    var candles = _client.GetCandlesHistoryBatchAsync(pairs, CandlePriceType.Trades, CandleTimeInterval.Month, new DateTime(2017, 1, 1), new DateTime(2018, 1, 1));
                    //Assert.AreEqual(candles.Keys.Count(), 18);
                });
            }
        }
    }
}
