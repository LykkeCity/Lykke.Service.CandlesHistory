using JetBrains.Annotations;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    [UsedImplicitly]
    public class CandlesHistorySettings
    {        
        public AssetsCacheSettings AssetsCache { get; set; }
        public DbSettings Db { get; set; }
        public int MaxCandlesCountWhichCanBeRequested { get; set; }
        public ResourceMonitorSettings ResourceMonitor { get; set; }
    }
}
