using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    /// <summary>
    /// Monitors length of tasks queue.
    /// </summary>
    public class QueueMonitor : TimerPeriod
    {
        private readonly ICandlesPersistenceQueue _queue;
        private readonly ILog _log;
        private readonly int _warningLength;

        public QueueMonitor(ILog log, ICandlesPersistenceQueue queue, int warningLength)
            : base(nameof(QueueMonitor), (int)TimeSpan.FromMinutes(10).TotalMilliseconds, log)
        {
            _log = log;
            _warningLength = warningLength;
            _queue = queue;
        }

        public override async Task Execute()
        {
            var currentLength = _queue.PersistTasksQueueLength;
            if (currentLength > _warningLength)
            {
                await _log.WriteWarningAsync(nameof(QueueMonitor), nameof(Execute), "", $"Processing queue's size exceeded warning level ({_warningLength}) and now equals {currentLength}.");
            }
        }
    }
}