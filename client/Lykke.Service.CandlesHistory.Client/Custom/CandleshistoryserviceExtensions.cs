using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Client.Custom;
using Lykke.Service.CandlesHistory.Client.Models;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable 1573 // Parameter is not mentioned in XML comments. To avoid panic on extension methods.

// ReSharper disable once CheckNamespace
namespace Lykke.Service.CandlesHistory.Client
{
    // ReSharper disable once UnusedMember.Global
    public static partial class CandleshistoryserviceExtensions
    {
        // *** Candles History ***

        // ReSharper disable once UnusedMember.Global
        public static async Task<CandlesHistoryResponseModel> TryGetCandlesHistoryAsync(
            this ICandleshistoryservice service, string assetPairId, CandlePriceType priceType,
            CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await service.GetCandlesHistoryOrErrorAsync(assetPairId, priceType, timeInterval, fromMoment,
                toMoment, cancellationToken);

            return result as CandlesHistoryResponseModel;
        }

        // ReSharper disable once UnusedMember.Global
        public static async Task<CandlesHistoryResponseModel> GetCandlesHistoryAsync(
            this ICandleshistoryservice service, string assetPairId, CandlePriceType priceType,
            CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await service.GetCandlesHistoryOrErrorAsync(assetPairId, priceType, timeInterval, fromMoment,
                toMoment, cancellationToken);

            switch (result)
            {
                case CandlesHistoryResponseModel candlesHistoryResponseModel:
                    return candlesHistoryResponseModel;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result?.GetType()}");
        }

        // *** Candles History Batch ***

        // ReSharper disable once UnusedMember.Global
        public static async Task<IReadOnlyDictionary<string, CandlesHistoryResponseModel>> TryGetCandlesHistoryBatchAsync(
            this ICandleshistoryservice service,
            IList<string> assetPairs,
            CandlePriceType priceType,
            CandleTimeInterval timeInterval,
            DateTime fromMoment,
            DateTime toMoment,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await service.GetCandlesHistoryBatchOrErrorAsync(new GetCandlesHistoryBatchRequest(priceType, timeInterval, fromMoment, toMoment, assetPairs), cancellationToken);

            return result as IReadOnlyDictionary<string, CandlesHistoryResponseModel>;
        }

        // ReSharper disable once UnusedMember.Global
        public static async Task<IReadOnlyDictionary<string, CandlesHistoryResponseModel>> GetCandlesHistoryBatchAsync(
            this ICandleshistoryservice service,
            IList<string> assetPairs,
            CandlePriceType priceType,
            CandleTimeInterval timeInterval,
            DateTime fromMoment,
            DateTime toMoment,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await service.GetCandlesHistoryBatchOrErrorAsync(new GetCandlesHistoryBatchRequest(priceType, timeInterval, fromMoment, toMoment, assetPairs), cancellationToken);

            switch (result)
            {
                case IReadOnlyDictionary<string, CandlesHistoryResponseModel> candlesHistoryResponseModel:
                    return candlesHistoryResponseModel;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result?.GetType()}");
        }

        // ReSharper disable once UnusedMember.Global
        public static async Task<CandlesHistoryResponseModel> GetCandlesHistoryFromDbAsync(this ICandleshistoryservice service, string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await service.GetCandlesHistoryFromDbOrErrorAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, cancellationToken);

            switch (result)
            {
                case CandlesHistoryResponseModel candlesHistoryResponseModel:
                    return candlesHistoryResponseModel;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result?.GetType()}");
        }

        // *** 24H Volumes ***

        /// <summary>
        /// Tries to get summary trading volumes for the specified asset pair for the last 24 hours. Returns -null- if failed.
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        public static async Task<TradingVolumeResponseModel> TryGet24HVolumesAsync(this ICandleshistoryservice service,
            string assetPairId)
        {
            var result = await service.Get24HVolumesWithHttpMessagesAsync(assetPairId);

            return result.Body as TradingVolumeResponseModel;
        }

        /// <summary>
        /// Gets summary trading volumes for the specified asset pair for the last 24 hours.
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static async Task<TradingVolumeResponseModel> Get24HVolumesAsync(this ICandleshistoryservice service,
            string assetPairId)
        {
            var result = await service.Get24HVolumesWithHttpMessagesAsync(assetPairId);

            switch (result.Body)
            {
                case TradingVolumeResponseModel tradingVolumeResponseMode:
                    return tradingVolumeResponseMode;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result.GetType()}");
        }

        /// <summary>
        /// Tries to get summary trading volumes for all the supported asset pairs for the last 24 hours. Returns -null- if failed.
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, TradingVolumeResponseModel>> TryGetAll24HVolumesAsync(
            this ICandleshistoryservice service)
        {
            var result = await service.GetAll24HVolumesWithHttpMessagesAsync();

            return result.Body as IReadOnlyDictionary<string, TradingVolumeResponseModel>;
        }

        /// <summary>
        /// Gets summary trading volumes for all the supported asset pairs for the last 24 hours.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public static async Task<IReadOnlyDictionary<string, TradingVolumeResponseModel>> GetAll24HVolumesAsync(
            this ICandleshistoryservice service)
        {
            var result = await service.GetAll24HVolumesWithHttpMessagesAsync();

            switch (result.Body)
            {
                case IReadOnlyDictionary<string, TradingVolumeResponseModel> multipleTradingVolumeResponse:
                    return multipleTradingVolumeResponse;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result.GetType()}");
        }

        // *** Today Volumes ***

        /// <summary>
        /// Tries to get summary trading volumes for the specified asset pair for the time period since 00:00:00 today (UTC). Returns -null- if failed.
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        public static async Task<TradingVolumeResponseModel> TryGetTodayVolumesAsync(
            this ICandleshistoryservice service,
            string assetPairId)
        {
            var result = await service.GetTodayVolumesWithHttpMessagesAsync(assetPairId);

            return result.Body as TradingVolumeResponseModel;
        }

        /// <summary>
        /// Gets summary trading volumes for the specified asset pair for the time period since 00:00:00 today (UTC).
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        public static async Task<TradingVolumeResponseModel> GetTodayVolumesAsync(this ICandleshistoryservice service,
            string assetPairId)
        {
            var result = await service.GetTodayVolumesWithHttpMessagesAsync(assetPairId);

            switch (result.Body)
            {
                case TradingVolumeResponseModel tradingVolumeResponseModel:
                    return tradingVolumeResponseModel;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result.GetType()}");
        }

        /// <summary>
        /// Tries to get summary trading volumes for all the supported asset pairs for the time period since 00:00:00 today (UTC). Returns -null- if failed.
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, TradingVolumeResponseModel>> TryGetAllTodayVolumesAsync(
            this ICandleshistoryservice service)
        {
            var result = await service.GetAllTodayVolumesWithHttpMessagesAsync();

            return result.Body as IReadOnlyDictionary<string, TradingVolumeResponseModel>;
        }

        /// <summary>
        /// Gets summary trading volumes for all the supported asset pairs for the time period since 00:00:00 today (UTC).
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, TradingVolumeResponseModel>> GetAllTodayVolumesAsync(
            this ICandleshistoryservice service)
        {
            var result = await service.GetAllTodayVolumesWithHttpMessagesAsync();

            switch (result.Body)
            {
                case IReadOnlyDictionary<string, TradingVolumeResponseModel> multipleTradingVolumeResponse:
                    return multipleTradingVolumeResponse;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result.GetType()}");
        }

        // *** Last Trade Price ***

        /// <summary>
        /// Tries to get the last trade price for the specified asset pair. The depth of search - 12 months since the current date (UTC). Returns -null- if failed.
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        public static async Task<LastTradePriceResponseModel> TryGetLastTradePriceAsync(
            this ICandleshistoryservice service,
            string assetPairId)
        {
            var result = await service.GetLastTradePriceWithHttpMessagesAsync(assetPairId);

            return result.Body as LastTradePriceResponseModel;
        }

        /// <summary>
        /// Gets the last trade price for the specified asset pair. The depth of search - 12 months since the current date (UTC).
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        public static async Task<LastTradePriceResponseModel> GetLastTradePriceAsync(
            this ICandleshistoryservice service,
            string assetPairId)
        {
            var result = await service.GetLastTradePriceWithHttpMessagesAsync(assetPairId);

            switch (result.Body)
            {
                case LastTradePriceResponseModel lastTradePriceResponseModel:
                    return lastTradePriceResponseModel;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result.GetType()}");
        }

        /// <summary>
        /// Tries to get the last trade price for all the supported asset pairs. The depth of search - 12 months since the current date (UTC). Returns -null- if failed.
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, LastTradePriceResponseModel>>
            TryGetAllLastTradePricesAsync(this ICandleshistoryservice service)
        {
            var result = await service.GetAllLastTradePricesWithHttpMessagesAsync();

            return result.Body as IReadOnlyDictionary<string, LastTradePriceResponseModel>;
        }

        /// <summary>
        /// Gets the last trade price for all the supported asset pairs. The depth of search - 12 months since the current date (UTC).
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, LastTradePriceResponseModel>> GetAllLastTradePriceAsync(
            this ICandleshistoryservice service)
        {
            var result = await service.GetAllLastTradePricesWithHttpMessagesAsync();

            switch (result.Body)
            {
                case IReadOnlyDictionary<string, LastTradePriceResponseModel> multipleLastTradePriceResponse:
                    return multipleLastTradePriceResponse;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result.GetType()}");
        }

        // *** Trade Price Today Change ***

        /// <summary>
        /// Tries to get the relative change for trade price for the specified asset pair and the time period since 00:00:00 today (UTC). Returns -null- if failed.
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        public static async Task<TradePriceChangeResponseModel> TryGetTradePriceTodayChangeAsync(
            this ICandleshistoryservice service,
            string assetPairId)
        {
            var result = await service.GetTradePriceTodayChangeWithHttpMessagesAsync(assetPairId);

            return result.Body as TradePriceChangeResponseModel;
        }

        /// <summary>
        /// Gets the relative change for trade price for the specified asset pair and the time period since 00:00:00 today (UTC).
        /// </summary>
        /// <param name="assetPairId">Asset pair ID.</param>
        public static async Task<TradePriceChangeResponseModel> GetTradePriceTodayChangeAsync(
            this ICandleshistoryservice service,
            string assetPairId)
        {
            var result = await service.GetTradePriceTodayChangeWithHttpMessagesAsync(assetPairId);

            switch (result.Body)
            {
                case TradePriceChangeResponseModel tradePriceChangeResponseModel:
                    return tradePriceChangeResponseModel;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result.GetType()}");
        }

        /// <summary>
        /// Tries to get the relative change for trade price for all supported asset pairs and the time period since 00:00:00 today (UTC). Returns -null- if failed.
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, TradePriceChangeResponseModel>>
            TryGetAllTradePriceTodayChangeAsync(this ICandleshistoryservice service)
        {
            var result = await service.GeAllTradePriceTodayChangeWithHttpMessagesAsync();

            return result.Body as IReadOnlyDictionary<string, TradePriceChangeResponseModel>;
        }

        /// <summary>
        /// Gets the relative change for trade price for all supported asset pairs and the time period since 00:00:00 today (UTC).
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, TradePriceChangeResponseModel>> GetAllTradePriceTodayChangeAsync(
            this ICandleshistoryservice service)
        {
            var result = await service.GeAllTradePriceTodayChangeWithHttpMessagesAsync();

            switch (result.Body)
            {
                case IReadOnlyDictionary<string, TradePriceChangeResponseModel> multipleTradePriceChangeResponse:
                    return multipleTradePriceChangeResponse;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result.GetType()}");
        }
    }
}
