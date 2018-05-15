using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Models;
using Lykke.Service.CandlesHistory.Models.CandlesHistory;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.CandlesHistory.Controllers
{
    /// <summary>
    /// A controller for trading data.
    /// </summary>
    [Route("api/[controller]")]
    public class TradingDataController : Controller
    {
        private readonly ICandlesManager _candlesManager;
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly Dictionary<string, string> _candleHistoryAssetConnections;
        private readonly IShutdownManager _shutdownManager;
        private readonly ILog _log;

        #region Initialization

        public TradingDataController(
            ICandlesManager candlesManager,
            IAssetPairsManager assetPairsManager,
            Dictionary<string, string> candleHistoryAssetConnections,
            IShutdownManager shutdownManager,
            ILog log)
        {
            _candlesManager = candlesManager ?? throw new ArgumentNullException(nameof(assetPairsManager));
            _assetPairsManager = assetPairsManager ?? throw new ArgumentNullException(nameof(assetPairsManager));
            _candleHistoryAssetConnections = candleHistoryAssetConnections ?? throw new ArgumentNullException(nameof(candleHistoryAssetConnections));
            _shutdownManager = shutdownManager ?? throw new ArgumentNullException(nameof(shutdownManager));

            _log = log?.CreateComponentScope(nameof(TradingDataController)) ?? throw new ArgumentNullException(nameof(log));
        }

        #endregion

        #region TradingVolumes

        /// <summary>
        /// Gets summary trading volumes for the specified asset pair for the last 24 hours.
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        [HttpGet("24h-trading-volumes/{assetPairId}")]
        [SwaggerOperation("Get24HVolumes")]
        [ProducesResponseType(typeof(TradingVolumeResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Get24HVolumesAsync(string assetPairId)
        {
            if (!CheckupServiceState(out var failedCheckupResponse))
                return failedCheckupResponse;

            if (!CheckupAssetPair(assetPairId, out failedCheckupResponse))
                return failedCheckupResponse;

            // ---

            try
            {
                var (tradingVolume, oppositeTradingVolume) = await _candlesManager.GetSummaryTradingVolumesAsync(assetPairId, CandleTimeInterval.Hour, 24);

                return Ok(new TradingVolumeResponseModel(tradingVolume, oppositeTradingVolume));
            }
            catch (Exception ex)
            {
                return await NegativeResponseOnExceptionAsync(ex, assetPairId);
            }
        }

        /// <summary>
        /// Gets summary trading volumes for all the supported asset pairs for the last 24 hours.
        /// </summary>
        [HttpGet("24h-trading-volumes/all-pairs")]
        [SwaggerOperation("GetAll24HVolumes")]
        [ProducesResponseType(typeof(Dictionary<string, TradingVolumeResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAll24HVolumesAsync()
        {
            if (!CheckupServiceState(out var failedCheckupResponse))
                return failedCheckupResponse;

            // ---

            try
            {
                var assetPairs = await _assetPairsManager.GetAllEnabledAsync();

                // Querying candles for different asset pairs in parallel style
                var candleQueryTasks = assetPairs
                    .Where(assetPair => CheckupAssetPair(assetPair.Id))
                    .ToDictionary(assetPair => assetPair.Id, 
                        assetPair => _candlesManager.GetSummaryTradingVolumesAsync(assetPair.Id, CandleTimeInterval.Hour, 24));

                await Task.WhenAll(candleQueryTasks.Values);

                var result = candleQueryTasks
                    .ToDictionary(item => item.Key,
                        item => new TradingVolumeResponseModel(item.Value.Result));

                return Ok(result);
            }
            catch (Exception ex)
            {
                return await NegativeResponseOnExceptionAsync(ex);
            }
        }

        /// <summary>
        /// Gets summary trading volumes for the specified asset pair for the time period since 00:00:00 today (UTC).
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        [HttpGet("today-trading-volumes/{assetPairId}")]
        [SwaggerOperation("GetTodayVolumes")]
        [ProducesResponseType(typeof(TradingVolumeResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetTodayVolumesAsync(string assetPairId)
        {
            if (!CheckupServiceState(out var failedCheckupResponse))
                return failedCheckupResponse;

            if (!CheckupAssetPair(assetPairId, out failedCheckupResponse))
                return failedCheckupResponse;

            // ---

            try
            {
                var (tradingVolume, oppositeTradingVolume) = await _candlesManager.GetSummaryTradingVolumesAsync(assetPairId, CandleTimeInterval.Day, 0); // Today only

                return Ok(new TradingVolumeResponseModel(tradingVolume, oppositeTradingVolume));
            }
            catch (Exception ex)
            {
                return await NegativeResponseOnExceptionAsync(ex, assetPairId);
            }
        }

        /// <summary>
        /// Gets summary trading volumes for all the supported asset pairs for the time period since 00:00:00 today (UTC).
        /// </summary>
        [HttpGet("today-trading-volumes/all-pairs")]
        [SwaggerOperation("GetAllTodayVolumes")]
        [ProducesResponseType(typeof(Dictionary<string, TradingVolumeResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAllTodayVolumesAsync()
        {
            if (!CheckupServiceState(out var failedCheckupResponse))
                return failedCheckupResponse;

            // ---

            try
            {
                var assetPairs = await _assetPairsManager.GetAllEnabledAsync();

                // Querying candles for different asset pairs in parallel style
                var candleQueryTasks = assetPairs
                    .Where(assetPair => CheckupAssetPair(assetPair.Id))
                    .ToDictionary(assetPair => assetPair.Id,
                        assetPair => _candlesManager.GetSummaryTradingVolumesAsync(assetPair.Id, CandleTimeInterval.Day, 0)); // Today only

                await Task.WhenAll(candleQueryTasks.Values);

                var result = candleQueryTasks
                    .ToDictionary(item => item.Key,
                        item => new TradingVolumeResponseModel(item.Value.Result));

                return Ok(result);
            }
            catch (Exception ex)
            {
                return await NegativeResponseOnExceptionAsync(ex);
            }
        }

        #endregion

        #region LastPrice

        /// <summary>
        /// Gets the last trade price for the specified asset pair. The depth of search - 5 months since the current date (UTC).
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        [HttpGet("last-trade-price/{assetPairId}")]
        [SwaggerOperation("GetLastTradePrice")]
        [ProducesResponseType(typeof(LastTradePriceResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetLastTradePriceAsync(string assetPairId)
        {
            if (!CheckupServiceState(out var failedCheckupResponse))
                return failedCheckupResponse;

            if (!CheckupAssetPair(assetPairId, out failedCheckupResponse))
                return failedCheckupResponse;

            // ---

            try
            {
                var lastPrice = await _candlesManager.GetLastTradePriceAsync(assetPairId, CandleTimeInterval.Month, 5);

                return Ok(new LastTradePriceResponseModel(lastPrice));
            }
            catch (Exception ex)
            {
                return await NegativeResponseOnExceptionAsync(ex, assetPairId);
            }
        }

        /// <summary>
        /// Gets the last trade price for all the supported asset pairs. The depth of search - 5 months since the current date (UTC).
        /// </summary>
        [HttpGet("last-trade-price/all-pairs")]
        [SwaggerOperation("GetAllLastTradePrices")]
        [ProducesResponseType(typeof(Dictionary<string, LastTradePriceResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAllLastTradePricesAsync()
        {
            if (!CheckupServiceState(out var failedCheckupResponse))
                return failedCheckupResponse;

            // ---

            try
            {
                var assetPairs = await _assetPairsManager.GetAllEnabledAsync();

                // Querying candles for different asset pairs in parallel style
                var candleQueryTasks = assetPairs
                    .Where(assetPair => CheckupAssetPair(assetPair.Id))
                    .ToDictionary(assetPair => assetPair.Id,
                        assetPair => _candlesManager.GetLastTradePriceAsync(assetPair.Id, CandleTimeInterval.Month, 5));

                await Task.WhenAll(candleQueryTasks.Values);

                var result = candleQueryTasks
                    .ToDictionary(item => item.Key,
                        item => new LastTradePriceResponseModel(item.Value.Result));

                return Ok(result);
            }
            catch (Exception ex)
            {
                return await NegativeResponseOnExceptionAsync(ex);
            }
        }

        /// <summary>
        /// Gets the relative change for trade price for the specified asset pair and the time period since 00:00:00 today (UTC).
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        [HttpGet("today-trade-price-change/{assetPairId}")]
        [SwaggerOperation("GetTradePriceTodayChange")]
        [ProducesResponseType(typeof(TradePriceChangeResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetTradePriceTodayChangeAsync(string assetPairId)
        {
            if (!CheckupServiceState(out var failedCheckupResponse))
                return failedCheckupResponse;

            if (!CheckupAssetPair(assetPairId, out failedCheckupResponse))
                return failedCheckupResponse;

            // ---

            try
            {
                var tradePriceChange = await _candlesManager.GetTradePriceChangeAsync(assetPairId, CandleTimeInterval.Day, 0); // Today only

                return Ok(new TradePriceChangeResponseModel(tradePriceChange));
            }
            catch (Exception ex)
            {
                return await NegativeResponseOnExceptionAsync(ex, assetPairId);
            }
        }

        /// <summary>
        /// Gets the relative change for trade price for all supported asset pairs and the time period since 00:00:00 today (UTC).
        /// </summary>
        [HttpGet("today-trade-price-change/all-pairs")]
        [SwaggerOperation("GeAlltTradePriceTodayChange")]
        [ProducesResponseType(typeof(Dictionary<string, TradePriceChangeResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAllTradePriceTodayChangeAsync()
        {
            if (!CheckupServiceState(out var failedCheckupResponse))
                return failedCheckupResponse;

            // ---

            try
            {
                var assetPairs = await _assetPairsManager.GetAllEnabledAsync();

                // Querying candles for different asset pairs in parallel style
                var candleQueryTasks = assetPairs
                    .Where(assetPair => CheckupAssetPair(assetPair.Id))
                    .ToDictionary(assetPair => assetPair.Id,
                        assetPair => _candlesManager.GetTradePriceChangeAsync(assetPair.Id, CandleTimeInterval.Day, 0)); // Today only

                await Task.WhenAll(candleQueryTasks.Values);

                var result = candleQueryTasks
                    .ToDictionary(item => item.Key,
                        item => new TradePriceChangeResponseModel(item.Value.Result));

                return Ok(result);
            }
            catch (Exception ex)
            {
                return await NegativeResponseOnExceptionAsync(ex);
            }
        }

        #endregion

        #region Private

        private bool CheckupServiceState(out IActionResult failedCheckupResponse)
        {
            if (_shutdownManager.IsShuttingDown)
            {
                failedCheckupResponse = StatusCode((int) HttpStatusCode.ServiceUnavailable,
                    ErrorResponse.Create("Service availability", "Service is shutting down"));
                return false;
            }

            if (_shutdownManager.IsShuttedDown)
            {
                failedCheckupResponse = StatusCode((int) HttpStatusCode.ServiceUnavailable,
                    ErrorResponse.Create("Service availability", "Service is shutted down"));
                return false;
            }

            failedCheckupResponse = default;
            return true;
        }

        private bool CheckupAssetPair(string assetPairId, out IActionResult failedCheckupResponse)
        {
            if (string.IsNullOrWhiteSpace(assetPairId))
            {
                failedCheckupResponse = BadRequest(ErrorResponse.Create(nameof(assetPairId), 
                    "Asset pair is required"));
                return false;
            }

            if (!_candleHistoryAssetConnections.ContainsKey(assetPairId))
            {
                failedCheckupResponse = BadRequest(ErrorResponse.Create(assetPairId,
                    "Asset pair is not configured"));
                return false;
            }

            if (_assetPairsManager.TryGetEnabledPairAsync(assetPairId).GetAwaiter().GetResult() == null)
            {
                failedCheckupResponse = BadRequest(ErrorResponse.Create(assetPairId, 
                    "Asset pair not found or disabled"));
                return false;
            }

            failedCheckupResponse = default;
            return true;
        }

        private bool CheckupAssetPair(string assetPairId)
        {
            return CheckupAssetPair(assetPairId, out var unused);
        }

        private async Task<IActionResult> NegativeResponseOnExceptionAsync(Exception ex, string assetPairId = null)
        {
            await _log.WriteErrorAsync(nameof(GetTradePriceTodayChangeAsync), assetPairId ?? string.Empty, ex);
            return StatusCode((int)HttpStatusCode.InternalServerError, ErrorResponse.Create("Internal", $"Internal service error: {ex.Message}"));
        }

        #endregion

    }
}
