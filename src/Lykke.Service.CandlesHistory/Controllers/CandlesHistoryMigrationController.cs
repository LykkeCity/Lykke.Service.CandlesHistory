using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.CandlesHistory.Services.HistoryMigration;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.CandlesHistory.Controllers
{
    [Route("api/[controller]")]
    public class CandlesHistoryMigrationController : Controller
    {
        private readonly CandlesMigrationManager _candlesMigrationManager;

        public CandlesHistoryMigrationController(CandlesMigrationManager candlesMigrationManager)
        {
            _candlesMigrationManager = candlesMigrationManager;
        }

        [HttpPost]
        [Route("test")]
        public IActionResult Test()
        {
            var generator = new MissedCandlesGenerator();

            var candles = generator.GenerateCandles(
                    new AssetPairResponseModel
                    {
                        Id = "EURUSD",
                        Accuracy = 5
                    },
                    PriceType.Ask,
                    new DateTime(2017, 08, 16, 15, 14, 49, DateTimeKind.Utc),
                    new DateTime(2017, 08, 16, 15, 14, 57, DateTimeKind.Utc),
                    0,
                    1.17046,
                    0)
                .ToArray();

            return Ok(candles);
        }

        [HttpPost]
        [Route("random/{assetPair}")]
        public async Task<IActionResult> Random(string assetPair)
        {
            var result = await _candlesMigrationManager.RandomAsync(assetPair, 
                new DateTime(2017, 10, 26, 00, 00, 00, DateTimeKind.Utc).AddSeconds(-1),
                new DateTime(2017, 10, 29, 00, 00, 00, DateTimeKind.Utc),
                1.3212,
                1.1721,
                0.02);
            return Ok(result);
        }

        [HttpPost]
        [Route("{assetPair}")]
        public async Task<IActionResult> Migrate(string assetPair)
        {
            var result = await _candlesMigrationManager.MigrateAsync(assetPair);
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
