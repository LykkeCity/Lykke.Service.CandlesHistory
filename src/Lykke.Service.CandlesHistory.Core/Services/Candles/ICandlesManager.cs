using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesManager
    {
        void ProcessCandle(ICandle candle);
        Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment);
    }
}
