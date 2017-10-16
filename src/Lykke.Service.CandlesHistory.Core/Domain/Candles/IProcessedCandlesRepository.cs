using System;
using System.Threading.Tasks;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public interface IProcessedCandle
    {
        string AssetPair { get; }
        DateTime Date { get; }
    }

    public interface IProcessedCandlesRepository
    {
        Task<IProcessedCandle> GetProcessedCandleAsync(string assetPair, PriceType priceType);
        Task AddProcessedCandleAsync(string assetPair, PriceType priceType, DateTime date);
    }
}
