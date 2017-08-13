using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    /// <summary>
    /// Monitors length of tasks queue.
    /// </summary>
    public class QueueMonitor : TimerPeriod
    {
        private readonly ICandlesPersistenceQueue _queue;
        private readonly ApplicationSettings.QueueMonitorSettings _setting;
        private readonly ILog _log;

        public QueueMonitor(
            ILog log, 
            ICandlesPersistenceQueue queue, 
            ApplicationSettings.QueueMonitorSettings setting)
            : base(nameof(QueueMonitor), (int)TimeSpan.FromMinutes(10).TotalMilliseconds, log)
        {
            _log = log;
            _queue = queue;
            _setting = setting;
        }

        public override async Task Execute()
        {
            var currentBatchesQueueLength = _queue.BatchesToPersistQueueLength;
            var currentCandlesQueueLength = _queue.CandlesToDispatchQueueLength;

            if (currentBatchesQueueLength > _setting.BatchesToPersistQueueLengthWarning ||
                currentCandlesQueueLength > _setting.CandlesToDispatchQueueLengthWarning)
            {
                await _log.WriteWarningAsync(
                    nameof(QueueMonitor),
                    nameof(Execute),
                    "",
                    $@"One of processing queue's size exceeded warning level. 
Candles batches to persist queue length={currentBatchesQueueLength} (warning={_setting.BatchesToPersistQueueLengthWarning}.
Candles to dispatch queue length={currentCandlesQueueLength} (warning={_setting.CandlesToDispatchQueueLengthWarning}");
            }
        }
    }
}