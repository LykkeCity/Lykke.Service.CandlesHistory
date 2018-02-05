namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class CandlesHistorySettings
    {        
        public AssetsCacheSettings AssetsCache { get; set; }
        public DbSettings Db { get; set; }
        public int HistoryTicksCacheSize { get; set; }
        public int MaxCandlesCountWhichCanBeRequested { get; set; }
    }
}
