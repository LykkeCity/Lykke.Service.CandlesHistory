using System;
using Common.Log;
using Lykke.Common;
using Lykke.Common.Log;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Models.IsAlive;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.CandlesHistory.Controllers
{
    /// <summary>
    /// Controller to test service is alive
    /// </summary>
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly IShutdownManager _shutdownManager;

        public IsAliveController(IShutdownManager shutdownManager)
        {
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
                Name = AppEnvironment.Name,
                Version = AppEnvironment.Version,
                Env = AppEnvironment.EnvInfo,
                IsShuttingDown = _shutdownManager.IsShuttingDown,
                IsShuttedDown = _shutdownManager.IsShuttedDown
            };
        }
    }
}
