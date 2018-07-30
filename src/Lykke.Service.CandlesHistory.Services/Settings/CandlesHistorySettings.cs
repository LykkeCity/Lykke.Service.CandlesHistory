using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    [UsedImplicitly]
    public class CandlesHistorySettings
    {
        public AssetsCacheSettings AssetsCache { get; set; }
        public DbSettings Db { get; set; }
        public int MaxCandlesCountWhichCanBeRequested { get; set; }
        [Optional, CanBeNull]
        public ResourceMonitorSettings ResourceMonitor { get; set; }
    }
}
