using System;
using System.Collections.Generic;

namespace Lykke.Service.CandlesHistory.Core
{
    public class ApplicationSettings
    {
        public CandlesHistorySettings CandlesHistory { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }

        public Dictionary<string, string> CandleHistoryAssetConnections { get; set; }

        public class CandlesHistorySettings
        {
            public DictionariesSettings Dictionaries { get; set; }
            public LogsSettings Logs { get; set; }
            public RabbitSettings QuoteFeedRabbit { get; set; }
            public int HistoryTicksCacheSize { get; set; }
        }

        public class DictionariesSettings
        {
            public string DbConnectionString { get; set; }
            public TimeSpan CacheExpirationPeriod { get; set; }
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