using System;
using Lykke.Service.CandlesHistory.Core.Services;

namespace Lykke.Service.CandlesHistory.Services
{
    public class Clock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}