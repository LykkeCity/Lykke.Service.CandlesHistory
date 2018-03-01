using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    [UsedImplicitly]
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnectionString { get; set; }
    }
}
