using System;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class QueueMonitorSettings
    {
        public int BatchesToPersistQueueLengthWarning { get; set; }
        public int CandlesToDispatchQueueLengthWarning { get; set; }
        public TimeSpan ScanPeriod { get; set; }
    }
}