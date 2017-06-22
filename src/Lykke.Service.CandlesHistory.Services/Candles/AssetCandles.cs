using System.Collections.Generic;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    internal class AssetCandles
    {
        public LinkedList<IFeedCandle> BidCandles { get; set; }
        public LinkedList<IFeedCandle> AskCandles { get; set; }
        public LinkedList<IFeedCandle> MidCandles { get; set; }
    }
}