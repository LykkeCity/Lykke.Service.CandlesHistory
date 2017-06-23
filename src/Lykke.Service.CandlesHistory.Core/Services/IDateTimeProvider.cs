using System;

namespace Lykke.Service.CandlesHistory.Core.Services
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}