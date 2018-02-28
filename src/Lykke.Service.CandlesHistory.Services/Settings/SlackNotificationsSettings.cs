using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class SlackNotificationsSettings
    {
        [AzureQueueCheck]
        public AzureQueueSettings AzureQueue { get; set; }
    }
}
