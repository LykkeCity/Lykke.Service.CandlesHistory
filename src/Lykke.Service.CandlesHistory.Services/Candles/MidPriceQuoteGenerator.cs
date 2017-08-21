using System;
using System.Collections.Concurrent;
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices.Model;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class MidPriceQuoteGenerator : IMidPriceQuoteGenerator
    {
        private class AssetPrice
        {
            public double Price { get; }
            public DateTime Timestamp { get; }

            public AssetPrice(double price, DateTime timestamp)
            {
                Price = price;
                Timestamp = timestamp;
            }
        }

        private class AssetState
        {
            public AssetPrice Ask { get; }
            public AssetPrice Bid { get; }

            public AssetState(AssetPrice ask, AssetPrice bid)
            {
                Ask = ask;
                Bid = bid;
            }
        }

        private readonly ConcurrentDictionary<string, AssetState> _assetQuoteStates;

        public MidPriceQuoteGenerator()
        {
            _assetQuoteStates = new ConcurrentDictionary<string, AssetState>();
        }

        public void Initialize(string assetPairId, bool isBid, double price, DateTime timestamp)
        {
            _assetQuoteStates.AddOrUpdate(
                assetPairId.Trim().ToUpper(),
                k => AddNewAssetState(new AssetPrice(price, timestamp), isBid),
                (k, oldState) => UpdateAssetState(oldState, new AssetPrice(price, timestamp), isBid));
        }

        public IQuote TryGenerate(IQuote quote, int assetPairAccuracy)
        {
            var assetPairId = quote.AssetPair.Trim().ToUpper();
            var state = _assetQuoteStates.AddOrUpdate(
                assetPairId,
                k => AddNewAssetState(new AssetPrice(quote.Price, quote.Timestamp), quote.IsBuy),
                (k, oldState) => UpdateAssetState(oldState, new AssetPrice(quote.Price, quote.Timestamp), quote.IsBuy));

            return TryCreateMidQuote(assetPairId, state, assetPairAccuracy);
        }

        private static AssetState AddNewAssetState(AssetPrice assetPrice, bool isBid)
        {
            return isBid ? new AssetState(null, assetPrice) : new AssetState(assetPrice, null);
        }

        private static AssetState UpdateAssetState(AssetState oldState, AssetPrice assetPrice, bool isBid)
        {
            return isBid ? new AssetState(oldState.Ask, assetPrice) : new AssetState(assetPrice, oldState.Bid);
        }

        private static IQuote TryCreateMidQuote(string assetPairId, AssetState assetState, int assetPairAccuracy)
        {
            if (assetState.Bid != null && assetState.Ask != null)
            {
                return new Quote
                {
                    AssetPair = assetPairId,
                    Price = Math.Round((assetState.Ask.Price + assetState.Bid.Price) / 2, assetPairAccuracy),
                    Timestamp = assetState.Ask.Timestamp >= assetState.Bid.Timestamp
                        ? assetState.Ask.Timestamp
                        : assetState.Bid.Timestamp
                };
            }

            return null;
        }
    }
}