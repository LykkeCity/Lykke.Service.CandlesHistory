using System;

namespace Lykke.Service.CandlesHistory.Core.Services
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}