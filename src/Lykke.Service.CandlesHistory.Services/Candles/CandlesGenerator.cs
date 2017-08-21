using System;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesGenerator : ICandlesGenerator
    {
        public IFeedCandle GenerateAskBidCandle(IQuote quote, TimeInterval timeInterval)
        {
            var intervalDate = quote.Timestamp.RoundTo(timeInterval);

            return quote.ToCandle(intervalDate);
        }

        public IFeedCandle GenerateMidCandle(IFeedCandle askCandle, IFeedCandle bidCandle, int assetPairAccuracy)
        {
            // This can occur if generated candle both too old to be cached and it wasn't persisted yet.
            // So, throws exception to retry later
            if (askCandle == null && bidCandle == null)
            {
                throw new ArgumentException("At least one of ask or bid candle should exists in order to generate mid price candle");
            }
            if (askCandle != null && askCandle.IsBuy)
            {
                throw new ArgumentException("Ask candle shouldn't be IsBuy", nameof(askCandle));
            }
            if (bidCandle != null && !bidCandle.IsBuy)
            {
                throw new ArgumentException("Bid candle should be IsBuy", nameof(bidCandle));
            }
            if (askCandle != null && bidCandle != null && askCandle.DateTime != bidCandle.DateTime)
            {
                throw new ArgumentException("Ask and bid candles should have the same DateTime");
            }

            if (askCandle != null && bidCandle != null)
            {
                return new AssetPairCandle
                {
                    DateTime = askCandle.DateTime,
                    Open = CalculateMidPrice(askCandle.Open, bidCandle.Open, assetPairAccuracy),
                    Close = CalculateMidPrice(askCandle.Close, bidCandle.Close, assetPairAccuracy),
                    Low = CalculateMidPrice(askCandle.Low, bidCandle.Low, assetPairAccuracy),
                    High = CalculateMidPrice(askCandle.High, bidCandle.High, assetPairAccuracy)
                };
            }

            if (askCandle != null)
            {
                return new AssetPairCandle
                {
                    DateTime = askCandle.DateTime,
                    Open = askCandle.Open,
                    Close = askCandle.Close,
                    Low = askCandle.Low,
                    High = askCandle.High
                };
            }

            return new AssetPairCandle
            {
                DateTime = bidCandle.DateTime,
                Open = bidCandle.Open,
                Close = bidCandle.Close,
                Low = bidCandle.Low,
                High = bidCandle.High
            };
        }

        private static double CalculateMidPrice(double askPrice, double bidPrice, int assetPairAccuracy)
        {
            return Math.Round((askPrice + bidPrice) / 2, assetPairAccuracy, MidpointRounding.AwayFromZero);
        }
    }
}