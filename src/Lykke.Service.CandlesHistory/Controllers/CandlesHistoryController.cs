using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Models;
using Lykke.Service.CandlesHistory.Models.CandlesHistory;
using Lykke.Service.CandlesHistory.Validation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        private readonly CandlesHistorySizeValidator _candlesHistorySizeValidator;

        public CandlesHistoryController(
            ICandlesManager candlesManager,
            IAssetPairsManager assetPairsManager,
            Dictionary<string, string> candleHistoryAssetConnections,
            IShutdownManager shutdownManager,
            CandlesHistorySizeValidator candlesHistorySizeValidator)
        {
            _candlesManager = candlesManager;
            _assetPairsManager = assetPairsManager;
            _candleHistoryAssetConnections = candleHistoryAssetConnections;
            _shutdownManager = shutdownManager;
            _candlesHistorySizeValidator = candlesHistorySizeValidator;
        }

        /// <summary>
        /// Pairs for which hisotry can be requested
        /// </summary>
        [HttpGet("availableAssetPairs")]
        [SwaggerOperation("GetAvailableAssetPairs")]
        [ProducesResponseType(typeof(string[]), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailableAssetPairs()
        {
            var assetPairs = await _assetPairsManager.GetAllEnabledAsync();

            return Ok(assetPairs
                .Where(p => _candleHistoryAssetConnections.ContainsKey(p.Id))
                .Select(p => p.Id));
        }

        [HttpPost("batch")]
        [SwaggerOperation("GetCandlesHistoryBatchOrError")]
        [ProducesResponseType(typeof(Dictionary<string, CandlesHistoryResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetCandlesHistoryBatch([FromBody] GetCandlesHistoryBatchRequest request)
        {
            if (_shutdownManager.IsShuttingDown)
            {
                return StatusCode((int)HttpStatusCode.ServiceUnavailable, ErrorResponse.Create("Service is shutting down"));
            }
            if (_shutdownManager.IsShuttedDown)
            {
                return StatusCode((int)HttpStatusCode.ServiceUnavailable, ErrorResponse.Create("Service is shutted down"));
            }

            if (request.AssetPairs == null || !request.AssetPairs.Any())
            {
                return Ok(new Dictionary<string, CandlesHistoryResponseModel>());
            }

            request.FromMoment = request.FromMoment.ToUniversalTime();
            request.ToMoment = request.ToMoment.ToUniversalTime();

            if (request.PriceType == CandlePriceType.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(request.PriceType), $"Price type should not be {CandlePriceType.Unspecified}"));
            }
            if (request.TimeInterval == CandleTimeInterval.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(request.TimeInterval), $"Time interval should not be {CandleTimeInterval.Unspecified}"));
            }
            if (request.FromMoment > request.ToMoment)
            {
                return BadRequest(ErrorResponse.Create("From date should be early or equal than To date"));
            }
            if (!_candlesHistorySizeValidator.CanBeRequested(request.FromMoment, request.ToMoment, request.TimeInterval, request.AssetPairs.Length))
            {
                return BadRequest(ErrorResponse.Create($"Too many candles requested at once. Amount of returned candles should not exceed {_candlesHistorySizeValidator.MaxCandlesCountWhichCanBeRequested}"));
            }

            var notConfiguerdAssetPairs = request.AssetPairs
                .Where(p => !_candleHistoryAssetConnections.ContainsKey(p))
                .ToArray();

            if (notConfiguerdAssetPairs.Any())
            {
                return BadRequest(
                    ErrorResponse.Create(nameof(request.AssetPairs), 
                    $"Asset pairs [{string.Join(", ", notConfiguerdAssetPairs)}] are not configured"));
            }

            var disabledAssetPairs = request.AssetPairs
                .Where(p => _assetPairsManager.TryGetEnabledPairAsync(p).GetAwaiter().GetResult() == null)
                .ToArray();

            if (disabledAssetPairs.Any())
            {
                return BadRequest(
                    ErrorResponse.Create(nameof(request.AssetPairs),
                        $"Asset pairs [{string.Join(", ", notConfiguerdAssetPairs)}] are not found or disabled"));
            }

            var allHistory = new ConcurrentDictionary<string, ICandle[]>();
            var tasks = request.AssetPairs.Select(assetPair => Task.Run(async () =>
            {
                var assetPairCandles = await _candlesManager.GetCandlesAsync(assetPair, request.PriceType,
                    request.TimeInterval, request.FromMoment, request.ToMoment);

                allHistory.TryAdd(assetPair, assetPairCandles.ToArray());
            }));

            await Task.WhenAll(tasks);

            return Ok(allHistory.ToDictionary(x => x.Key, x => new CandlesHistoryResponseModel
            {
                History = x.Value.Select(c => new CandlesHistoryResponseModel.Candle
                {
                    DateTime = c.Timestamp,
                    Open = c.Open,
                    Close = c.Close,
                    High = c.High,
                    Low = c.Low,
                    TradingVolume = c.TradingVolume,
                    TradingOppositeVolume = c.TradingOppositeVolume,
                    LastTradePrice = c.LastTradePrice
                })
            }));
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
        public async Task<IActionResult> GetCandlesHistory(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
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
            toMoment = toMoment.ToUniversalTime();

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
            if (fromMoment > toMoment)
            {
                return BadRequest(ErrorResponse.Create("From date should be early or equal than To date"));
            }
            if (!_candlesHistorySizeValidator.CanBeRequested(fromMoment, toMoment, timeInterval))
            {
                return BadRequest(ErrorResponse.Create($"Too many candles requested at once. Amount of returned candles should not exceed {_candlesHistorySizeValidator.MaxCandlesCountWhichCanBeRequested}"));
            }
            if (!_candleHistoryAssetConnections.ContainsKey(assetPairId))
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair is not configured"));
            }
            if (await _assetPairsManager.TryGetEnabledPairAsync(assetPairId) == null)
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair not found or disabled"));
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
                    Low = c.Low,
                    TradingVolume = c.TradingVolume,
                    TradingOppositeVolume = c.TradingOppositeVolume,
                    LastTradePrice = c.LastTradePrice
                })
            });
        }
    }
}
