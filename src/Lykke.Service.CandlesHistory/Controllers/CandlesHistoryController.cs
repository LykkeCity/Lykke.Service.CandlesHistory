using System;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.CandlesHistory.Controllers
{
    /// <summary>
    /// Controller for candles history
    /// </summary>
    [Route("api/[controller]")]
    public class CandlesHistoryController : Controller
    {
        private readonly ICandlesManager _candlesManager;

        public CandlesHistoryController(ICandlesManager candlesManager)
        {
            _candlesManager = candlesManager;
        }

        /// <summary>
        /// Asset's candles history
        /// </summary>
        /// <param name="assetId">Asset ID</param>
        /// <param name="priceType">Price type</param>
        /// <param name="timeInterval">Time interval</param>
        /// <param name="fromMoment">From moment in ISO 8601</param>
        /// <param name="toMoment">To moment in ISO 8601</param>
        [HttpGet("{assetId}/{priceType}/{timeInterval}/{fromMoment:datetime}/{toMoment:datetime}")]
        public IActionResult GetCandlesHistory(string assetId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            fromMoment = fromMoment.ToUniversalTime();
            toMoment = toMoment.ToUniversalTime();

            if (string.IsNullOrWhiteSpace(assetId))
            {
                return BadRequest(ErrorResponse.Create(nameof(assetId), "assetId is required"));
            }
            if (priceType == PriceType.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(timeInterval), $"Price type should not be {PriceType.Unspecified}"));
            }
            if (timeInterval == TimeInterval.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(timeInterval), $"Time interval should not be {TimeInterval.Unspecified}"));
            }
            if (fromMoment >= toMoment)
            {
                return BadRequest(ErrorResponse.Create("From date should be early than To date"));
            }

            var candles = _candlesManager.GetCandles(assetId, priceType, timeInterval, fromMoment, toMoment);

            return Ok(candles);
        }
    }
}