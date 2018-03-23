using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class ResourceMonitorSettings
    {
        public ResourceMonitorMode MonitorMode { get; set; }
        [Optional]
        public double CpuThreshold { get; set; }
        [Optional]
        public int RamThreshold { get; set; }
    }

    public enum ResourceMonitorMode
    {
        Off,
        AppInsightsOnly,
        AppInsightsWithLog
    }
}
