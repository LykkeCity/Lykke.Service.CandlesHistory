// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Models.IsAlive;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.PlatformAbstractions;
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
        public IsAliveResponse Get()
        {
            return new IsAliveResponse
            {
                Name = PlatformServices.Default.Application.ApplicationName,
                Version = PlatformServices.Default.Application.ApplicationVersion,
                Env = Program.EnvInfo,
                IsShuttingDown = _shutdownManager.IsShuttingDown,
                IsShuttedDown = _shutdownManager.IsShuttedDown
            };
        }
    }
}
