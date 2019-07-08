// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
        
        [Optional]
        public bool UseSerilog { get; set; }
    }
}
