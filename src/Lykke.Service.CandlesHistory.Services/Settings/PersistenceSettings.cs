using System;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class PersistenceSettings
    {
        public TimeSpan PersistPeriod { get; set; }
        public int MaxBatchSize { get; set; }
    }
}