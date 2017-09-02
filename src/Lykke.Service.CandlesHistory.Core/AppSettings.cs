﻿using System;
using System.Collections.Generic;
using Lykke.Service.Assets.Client.Custom;

namespace Lykke.Service.CandlesHistory.Core
{
    public class AppSettings
    {
        public CandlesHistorySettings CandlesHistory { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }

        public Dictionary<string, string> CandleHistoryAssetConnections { get; set; }

        public AssetsSettings Assets { get; set; }

        public class CandlesHistorySettings
        {
            public AssetsCacheSettings AssetsCache { get; set; }
            public RabbitSettingsWithDeadLetter CandlesSubscription { get; set; }
            public RabbitSettings FailedToPersistPublication { get; set; }
            public int HistoryTicksCacheSize { get; set; }
            public QueueMonitorSettings QueueMonitor { get; set; }
            public PersistenceSettings Persistence { get; set; }
            public DbSettings Db { get; set; }
        }
        
        public class DbSettings
        {
            public string LogsConnectionString { get; set; }
            public string SnapshotsConnectionString { get; set; }
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

        public class AssetsCacheSettings
        {
            public TimeSpan ExpirationPeriod { get; set; }
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