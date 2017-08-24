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
            public RabbitSettingsWithDeadLetter QuoteFeedRabbit { get; set; }
            public RabbitSettings FailedToPersistRabbit { get; set; }
            public int HistoryTicksCacheSize { get; set; }
            public QueueMonitorSettings QueueMonitor { get; set; }
            public PersistenceSettings Persistence { get; set; }
        }

        public class PersistenceSettings
        {
            public TimeSpan PersistPeriod { get; set; }
            public int MaxBatchSize { get; set; }
        }

        public class QueueMonitorSettings
        {
            public int BatchesToPersistQueueLengthWarning { get; set; }
            public int CandlesToDispatchQueueLengthWarning { get; set; }
            public TimeSpan ScanPeriod { get; set; }
        }

        public class DictionariesSettings
        {
            public string AssetsServiceUrl { get; set; }
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

        public class RabbitSettingsWithDeadLetter : RabbitSettings
        {
            public string DeadLetterExchangeName { get; set; }
        }
    }
}