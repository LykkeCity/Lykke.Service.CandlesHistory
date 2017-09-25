namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class SlackNotificationsSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }

        public int ThrottlingLimitSeconds { get; set; }
    }
}