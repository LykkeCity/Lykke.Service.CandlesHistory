using System;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class MigrationSettings
    {
        public int CandlesToDispatchLengthThrottlingThreshold { get; set; }
        public TimeSpan ThrottlingDelay { get; set; }
    }
}
