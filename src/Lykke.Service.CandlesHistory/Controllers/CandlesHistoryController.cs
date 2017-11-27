using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Services;
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
        private readonly Dictionary<string, string> _candleHistoryAssetConnections;
        private readonly IShutdownManager _shutdownManager;

        public CandlesHistoryController(
            ICandlesManager candlesManager,
            IAssetPairsManager assetPairsManager,
            Dictionary<string, string> candleHistoryAssetConnections,
            IShutdownManager shutdownManager)
        {
            _candlesManager = candlesManager;
            _assetPairsManager = assetPairsManager;
            _candleHistoryAssetConnections = candleHistoryAssetConnections;
            _shutdownManager = shutdownManager;
        }

        /// <summary>
        /// Asset's candles history
        /// </summary>
        /// <param name="assetPairId">Asset pair ID</param>
        /// <param name="priceType">Price type</param>
        /// <param name="timeInterval">Time interval</param>
        /// <param name="fromMoment">From moment in ISO 8601 (inclusive)</param>
        /// <param name="toMoment">To moment in ISO 8601 (exclusive)</param>
        [HttpGet("{assetPairId}/{priceType}/{timeInterval}/{fromMoment:datetime}/{toMoment?:datetime}")]
        [SwaggerOperation("GetCandlesHistoryOrError")]
        [ProducesResponseType(typeof(CandlesHistoryResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetCandlesHistory(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime? toMoment)
        {
            if (_shutdownManager.IsShuttingDown)
            {
                return StatusCode((int) HttpStatusCode.ServiceUnavailable, ErrorResponse.Create("Service is shutting down"));
            }

            if (_shutdownManager.IsShuttedDown)
            {
                return StatusCode((int) HttpStatusCode.ServiceUnavailable, ErrorResponse.Create("Service is shutted down"));
            }
            
            fromMoment = fromMoment.ToUniversalTime();
            toMoment = toMoment?.ToUniversalTime();
            assetPairId = assetPairId.ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(assetPairId))
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair is required"));
            }
            if (priceType == CandlePriceType.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(timeInterval), $"Price type should not be {CandlePriceType.Unspecified}"));
            }
            if (timeInterval == CandleTimeInterval.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(timeInterval), $"Time interval should not be {CandleTimeInterval.Unspecified}"));
            }
            if (toMoment.HasValue && fromMoment >= toMoment)
            {
                return BadRequest(ErrorResponse.Create("From date should be early than To date"));
            }
            if (!_candleHistoryAssetConnections.ContainsKey(assetPairId))
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
                    DateTime = c.Timestamp,
                    Open = c.Open,
                    Close = c.Close,
                    High = c.High,
                    Low = c.Low
                })
            });
        }
    }
}
