using JetBrains.Annotations;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    [UsedImplicitly]
    public class SlackNotificationsSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }
}
