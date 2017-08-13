using System;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Models.IsAlive;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.SwaggerGen.Annotations;

namespace Lykke.Service.CandlesHistory.Controllers
{
    /// <summary>
    /// Controller to test service is alive
    /// </summary>
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly ICandlesPersistenceQueue _persistenceQueue;
        private readonly IShutdownManager _shutdownManager;

        public IsAliveController(ICandlesPersistenceQueue persistenceQueue, IShutdownManager shutdownManager)
        {
            _persistenceQueue = persistenceQueue;
            _shutdownManager = shutdownManager;
        }

        /// <summary>
        /// Checks service is alive
        /// </summary>
        [HttpGet]
        [SwaggerOperation("IsAlive")]
        public IsAliveResponse Get()
        {
            return new IsAliveResponse
            {
                Version = PlatformServices.Default.Application.ApplicationVersion,
                Env = Environment.GetEnvironmentVariable("ENV_INFO"),
                BatchesToPersistQueueLength = _persistenceQueue.BatchesToPersistQueueLength,
                CandlesToDispatchQueueLength = _persistenceQueue.CandlesToDispatchQueueLength,
                IsShuttingDown = _shutdownManager.IsShuttingDown,
                IsShuttedDown = _shutdownManager.IsShuttedDown
            };
        }
    }
}