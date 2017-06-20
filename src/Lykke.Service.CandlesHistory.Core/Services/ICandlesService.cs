using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Services
{
    public interface ICandlesService
    {
        void AddQuote(IQuote quote);
    }
}