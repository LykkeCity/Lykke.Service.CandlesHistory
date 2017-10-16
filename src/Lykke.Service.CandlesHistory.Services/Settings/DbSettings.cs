namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class DbSettings
    {
        public string LogsConnectionString { get; set; }
        public string SnapshotsConnectionString { get; set; }
        public string FeedHistoryConnectionString { get; set; }
        public string ProcessedCandlesConnectionString { get; set; }
    }
}
