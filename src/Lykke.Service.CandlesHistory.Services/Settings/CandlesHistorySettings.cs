namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class CandlesHistorySettings
    {        
        public AssetsCacheSettings AssetsCache { get; set; }
        public RabbitSettings Rabbit { get; set; }
        public QueueMonitorSettings QueueMonitor { get; set; }
        public PersistenceSettings Persistence { get; set; }
        public DbSettings Db { get; set; }
        public MigrationSettings Migration { get; set; }
        public int HistoryTicksCacheSize { get; set; }
        public int MaxCandlesCountWhichCanBeRequested { get; set; }
        public string InstrumentationKey { get; set; }
    }
}
