﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Models;
using Lykke.Service.CandlesHistory.Models.CandlesHistory;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.CandlesHistory.Controllers
{
    /// <summary>
    /// A controller for candles history
    /// </summary>
    [Route("api/[controller]")]
    public class CandlesHistoryController : Controller
    {
        private readonly ICandlesManager _candlesManager;
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly Dictionary<string, string> _candleHistoryAssetConnections;

        #region Initialization

        public CandlesHistoryController(
            ICandlesManager candlesManager,
            IAssetPairsManager assetPairsManager,
            Dictionary<string, string> candleHistoryAssetConnections)
        {
            _candlesManager = candlesManager;
            _assetPairsManager = assetPairsManager;
            _candleHistoryAssetConnections = candleHistoryAssetConnections;
        }

        #endregion

        #region Public

        /// <summary>
        /// Pairs for which history can be requested
        /// </summary>
        [HttpGet("availableAssetPairs")]
        [SwaggerOperation("GetAvailableAssetPairs")]
        [ProducesResponseType(typeof(string[]), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        public async Task<IActionResult> GetAvailableAssetPairs()
        {
            var assetPairs = await _assetPairsManager.GetAllAsync();

            return Ok(assetPairs
                .Where(p => _candleHistoryAssetConnections.ContainsKey(p.Id))
                .Select(p => p.Id));
        }

        /// <summary>
        /// Shows history depth limits for all supported asset pairs.
        /// </summary>
        [HttpGet("availableAssetPairs/Depth")]
        [SwaggerOperation("GetAvailableAssetPairsHistoryDepth")]
        [ProducesResponseType(typeof(CandlesHistoryDepthResponseModel[]), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAvailableAssetPairsHistoryDepth()
        {
            try
            {
                var assetPairs = await _assetPairsManager.GetAllAsync();

                // Now we do select new history depth items in parallel  style for each asset pair,
                // but the depth item constructor itself executes 4 awaitable queries non-parallel.
                // I.e., if we have 10 asset pairs, we get here 10 parallel tasks with 4 sequential
                // data queries in each, but not 10 * 4 parallel tasks.
                var result = await Task.WhenAll(
                assetPairs
                    .Where(p => _candleHistoryAssetConnections.ContainsKey(p.Id))
                    .Select(async p => new CandlesHistoryDepthResponseModel
                    {
                        AssetPairId = p.Id,
                        OldestAskTimestamp = (await _candlesManager.TryGetOldestCandleAsync(p.Id, CandlePriceType.Ask, CandleTimeInterval.Sec))?.Timestamp,
                        OldestBidTimestamp = (await _candlesManager.TryGetOldestCandleAsync(p.Id, CandlePriceType.Bid, CandleTimeInterval.Sec))?.Timestamp,
                        OldestMidTimestamp = (await _candlesManager.TryGetOldestCandleAsync(p.Id, CandlePriceType.Mid, CandleTimeInterval.Sec))?.Timestamp,
                        OldestTradesTimestamp = (await _candlesManager.TryGetOldestCandleAsync(p.Id, CandlePriceType.Trades, CandleTimeInterval.Sec))?.Timestamp

                    })
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ErrorResponse.Create("Internal", ex.Message));
            }
        }

        /// <summary>
        /// Shows history depth limits for the specified asset pair if it is supported.
        /// </summary>
        [HttpGet("availableAssetPairs/Depth/{assetPairId}")]
        [SwaggerOperation("GetAssetPairHistoryDepth")]
        [ProducesResponseType(typeof(CandlesHistoryDepthResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAssetPairHistoryDepth(string assetPairId)
        {
            var resultTasks = Services.Candles.Constants.StoredPriceTypes
                .Select(pt => _candlesManager.TryGetOldestCandleAsync(assetPairId, pt, CandleTimeInterval.Sec))
                .ToList();

            try
            {
                var results = await Task.WhenAll(resultTasks);

                return Ok(new CandlesHistoryDepthResponseModel
                {
                    AssetPairId = assetPairId,
                    OldestAskTimestamp = results.FirstOrDefault(c => c.PriceType == CandlePriceType.Ask)?.Timestamp,
                    OldestBidTimestamp = results.FirstOrDefault(c => c.PriceType == CandlePriceType.Bid)?.Timestamp,
                    OldestMidTimestamp = results.FirstOrDefault(c => c.PriceType == CandlePriceType.Mid)?.Timestamp,
                    OldestTradesTimestamp = results.FirstOrDefault(c => c.PriceType == CandlePriceType.Trades)?.Timestamp
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ErrorResponse.Create("AssetPair", ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ErrorResponse.Create("Internal", ex.Message));
            }
        }

        [Obsolete]
        [HttpPost("batch")]
        [SwaggerOperation("GetCandlesHistoryBatchOrError")]
        [ProducesResponseType(typeof(Dictionary<string, CandlesHistoryResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetCandlesHistoryBatch([FromBody] GetCandlesHistoryBatchRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new Dictionary<string, CandlesHistoryResponseModel>());
            }

            if (cancellationToken.IsCancellationRequested || request.AssetPairs == null || !request.AssetPairs.Any())
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

            var notConfiguerdAssetPairs = request.AssetPairs
                .Where(p => !_candleHistoryAssetConnections.ContainsKey(p))
                .ToArray();

            if (notConfiguerdAssetPairs.Any())
            {
                return BadRequest(
                    ErrorResponse.Create(nameof(request.AssetPairs),
                    $"Asset pairs [{string.Join(", ", notConfiguerdAssetPairs)}] are not configured"));
            }

            var assetPairsTask = request.AssetPairs.Select(p => _assetPairsManager.TryGetAssetPairAsync(p)).ToArray();
            await Task.WhenAll(assetPairsTask);

            if (assetPairsTask.Any(t => t.Result == null))
            {
                var notFound = request.AssetPairs.Except(assetPairsTask.Select(p => p.Result?.Id).Where(p => p != null));
                return BadRequest(
                    ErrorResponse.Create(nameof(request.AssetPairs),
                        $"Asset pairs [{string.Join(", ", notFound)}] are not found"));
            }

            var allHistory = new Dictionary<string, CandlesHistoryResponseModel>();
            var tasks = new List<Task<IEnumerable<ICandle>>>();
            var activeSlot = _candlesManager.GetActiveSlot();
            foreach (var assetPair in request.AssetPairs)
            {
                allHistory[assetPair] = new CandlesHistoryResponseModel { History = Array.Empty<CandlesHistoryResponseModel.Candle>() };
                tasks.Add(_candlesManager.GetCandlesAsync(assetPair, request.PriceType, request.TimeInterval, request.FromMoment, request.ToMoment, activeSlot));
            }

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                var candles = task.Result.Select(c => new
                {
                    pair = c.AssetPairId,
                    model = new CandlesHistoryResponseModel.Candle
                    {
                        DateTime = c.Timestamp,
                        Open = c.Open,
                        Close = c.Close,
                        High = c.High,
                        Low = c.Low,
                        TradingVolume = c.TradingVolume,
                        TradingOppositeVolume = c.TradingOppositeVolume,
                        LastTradePrice = c.LastTradePrice
                    }
                }).ToArray();

                if (candles.Any())
                {
                    var p = candles[0].pair;
                    allHistory[p] = new CandlesHistoryResponseModel { History = candles.Select(c => c.model) };
                }
            }
            return Ok(allHistory);
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
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        public async Task<IActionResult> GetCandlesHistory(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
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
            if (!_candleHistoryAssetConnections.ContainsKey(assetPairId))
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair is not configured"));
            }
            if (await _assetPairsManager.TryGetAssetPairAsync(assetPairId) == null)
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair not found"));
            }

            var activeSlot = _candlesManager.GetActiveSlot();
            var candles = await _candlesManager.GetCandlesAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, activeSlot);

            // May return much less candles than it was requested or even an empty set of data for now the service looks
            // only through the cache (no persistent data is used).
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
        
        #endregion
    }
}
