// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.CandlesHistory.Core.Services;

namespace Lykke.Service.CandlesHistory.Services
{
    [UsedImplicitly]
    public class ShutdownManager : IShutdownManager
    {
        public bool IsShuttedDown { get; private set; }
        public bool IsShuttingDown { get; private set; }
        
        private readonly ILog _log;

        public ShutdownManager(
            ILog log)
        {
            _log = log.CreateComponentScope(nameof(ShutdownManager));
        }

        public async Task ShutdownAsync()
        {
            IsShuttingDown = true;

            await _log.WriteInfoAsync(nameof(ShutdownAsync), "", "Shutted down");

            IsShuttedDown = true;
            IsShuttingDown = false;
        }
    }
}
