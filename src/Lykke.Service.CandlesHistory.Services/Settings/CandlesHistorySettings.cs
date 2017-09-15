namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class CandlesHistorySettings
    {        
        public AssetsCacheSettings AssetsCache { get; set; }
        public RabbitSettingsWithDeadLetter CandlesSubscription { get; set; }
        public RabbitSettings FailedToPersistPublication { get; set; }
        public int HistoryTicksCacheSize { get; set; }
        public QueueMonitorSettings QueueMonitor { get; set; }
        public PersistenceSettings Persistence { get; set; }
        public DbSettings Db { get; set; }
    }
}