using Lykke.Service.CandlesHistory.Services.Candles.HistoryMigration;
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
        [Route("migrateCandles/{assetPair}")]
        public IActionResult Migration(string assetPair)
        {
            var result = _candlesMigrationManager.Migrate(assetPair);
            return Ok(result);
        }

        // TODO: Display progress
    }
}
