using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Models;
using Lykke.Service.CandlesHistory.Models.CandlesHistory;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;

namespace Lykke.Service.CandlesHistory.Controllers
{
    /// <summary>
    /// Controller for candles history
    /// </summary>
    [Route("api/[controller]")]
    public class CandlesHistoryController : Controller
    {
        private readonly ICandlesManager _candlesManager;
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly ApplicationSettings _settings;

        public CandlesHistoryController(ICandlesManager candlesManager, IAssetPairsManager assetPairsManager, ApplicationSettings settings)
        {
            _candlesManager = candlesManager;
            _assetPairsManager = assetPairsManager;
            _settings = settings;
        }

        /// <summary>
        /// Asset's candles history
        /// </summary>
        /// <param name="assetPairId">Asset pair ID</param>
        /// <param name="priceType">Price type</param>
        /// <param name="timeInterval">Time interval</param>
        /// <param name="fromMoment">From moment in ISO 8601 (inclusive)</param>
        /// <param name="toMoment">To moment in ISO 8601 (exclusive)</param>
        [HttpGet("{assetPairId}/{priceType}/{timeInterval}/{fromMoment:datetime}/{toMoment:datetime}")]
        [SwaggerOperation("GetCandlesHistoryOrError")]
        [ProducesResponseType(typeof(CandlesHistoryResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetCandlesHistory(string assetPairId, PriceType priceType, TimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            fromMoment = fromMoment.ToUniversalTime();
            toMoment = toMoment.ToUniversalTime();
            assetPairId = assetPairId.ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(assetPairId))
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair is required"));
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
            if (!_settings.CandleHistoryAssetConnections.ContainsKey(assetPairId))
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Connection string for asset pair not configured"));
            }
            if (await _assetPairsManager.TryGetEnabledPairAsync(assetPairId) == null)
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair not found in dictionary or disabled"));
            }

            var candles = await _candlesManager.GetCandlesAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment);

            return Ok(new CandlesHistoryResponseModel
            {
                History = candles.Select(c => new CandlesHistoryResponseModel.Candle
                {
                    DateTime = c.DateTime,
                    Open = c.Open,
                    Close = c.Close,
                    High = c.High,
                    Low = c.Low
                })
            });
        }
    }
}