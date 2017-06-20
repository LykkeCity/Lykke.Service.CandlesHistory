namespace Lykke.Service.CandlesHistory.Core
{
    public class ApplicationSettings
    {
        public CandlesHistorySettings CandlesHistory { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }

        public class CandlesHistorySettings
        {
            public LogsSettings Logs { get; set; }
            public RabbitSettings QuoteFeedRabbitSettings { get; set; }
        }

        public class LogsSettings
        {
            public string DbConnectionString { get; set; }
        }

        public class SlackNotificationsSettings
        {
            public AzureQueueSettings AzureQueue { get; set; }

            public int ThrottlingLimitSeconds { get; set; }
        }

        public class AzureQueueSettings
        {
            public string ConnectionString { get; set; }

            public string QueueName { get; set; }
        }

        public class RabbitSettings
        {
            public string ConnectionString { get; set; }
            public string ExchangeName { get; set; }
        }
    }
}