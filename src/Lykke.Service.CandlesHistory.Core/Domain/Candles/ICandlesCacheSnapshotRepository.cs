using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public interface ICandlesCacheSnapshotRepository
    {
        Task SaveAsync(KeyValuePair<string, LinkedList<IFeedCandle>>[] state);
        Task<IEnumerable<KeyValuePair<string, LinkedList<IFeedCandle>>>> TryGetAsync();
    }
}