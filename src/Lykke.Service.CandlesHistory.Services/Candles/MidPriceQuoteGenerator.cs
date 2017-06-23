using System;
using System.Collections.Concurrent;
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices.Model;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class MidPriceQuoteGenerator : IMidPriceQuoteGenerator
    {
        private struct AssetState
        {
            public IQuote Ask { get; }
            public IQuote Bid { get; }

            public AssetState(IQuote ask, IQuote bid)
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

        public IQuote TryGenerate(IQuote quote, int assetPairAccuracy)
        {
            var state = _assetQuoteStates.AddOrUpdate(
                quote.AssetPair,
                k => AddNewAssetState(quote),
                (k, oldState) => UpdateAssetState(oldState, quote));

            return TryCreateMidQuote(state, assetPairAccuracy);
        }

        private static AssetState AddNewAssetState(IQuote quote)
        {
            return quote.IsBuy ? new AssetState(null, quote) : new AssetState(quote, null);
        }

        private static AssetState UpdateAssetState(AssetState oldState, IQuote quote)
        {
            return quote.IsBuy ? new AssetState(oldState.Ask, quote) : new AssetState(quote, oldState.Bid);
        }

        private static IQuote TryCreateMidQuote(AssetState assetState, int assetPairAccuracy)
        {
            if (assetState.Bid != null && assetState.Ask != null)
            {
                return new Quote
                {
                    AssetPair = assetState.Bid.AssetPair,
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