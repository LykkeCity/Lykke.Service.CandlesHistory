using Lykke.Service.CandlesHistory.Services.HistoryMigration;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.CandlesHistory.Controllers
{
    [Route("api/[controller]")]
    public class CandlesHistoryMigrationController : Controller
    {
        private readonly CandlesMigrationManager _candlesMigrationManager;

        public CandlesHistoryMigrationController(
            CandlesMigrationManager candlesMigrationManager)
        {
            _candlesMigrationManager = candlesMigrationManager;
        }


        [HttpPost]
        [Route("{assetPair}")]
        public IActionResult Migrate(string assetPair)
        {
            var result = _candlesMigrationManager.Migrate(assetPair);
            return Ok(result);
        }

        [HttpGet]
        [Route("health")]
        public IActionResult Health()
        {
            return Ok(_candlesMigrationManager.Health);
        }

        [HttpGet]
        [Route("health/{assetPair}")]
        public IActionResult Health(string assetPair)
        {
            if (!_candlesMigrationManager.Health.ContainsKey(assetPair))
            {
                return NotFound();
            }

            return Ok(_candlesMigrationManager.Health[assetPair]);
        }
    }
}
