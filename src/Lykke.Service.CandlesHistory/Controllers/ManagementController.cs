using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;

namespace Lykke.Service.CandlesHistory.Controllers
{
    [Route("api/[controller]")]
    public class ManagementController : Controller
    {
        private readonly IShutdownManager _shutdownManager;

        public ManagementController(IShutdownManager shutdownManager)
        {
            _shutdownManager = shutdownManager;
        }

        [HttpPost]
        [SwaggerOperation("Shutdown")]
        public async Task<IActionResult> Shutdown()
        {
            await _shutdownManager.Shutdown();

            return NoContent();
        }
    }
}