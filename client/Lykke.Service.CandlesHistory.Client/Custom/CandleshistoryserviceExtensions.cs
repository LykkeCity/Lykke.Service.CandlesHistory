// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Client.Custom;
using Lykke.Service.CandlesHistory.Client.Models;

// ReSharper disable once CheckNamespace
namespace Lykke.Service.CandlesHistory.Client
{
    // ReSharper disable once UnusedMember.Global
    public static partial class CandleshistoryserviceExtensions
    {
        // ReSharper disable once UnusedMember.Global
        public static async Task<CandlesHistoryResponseModel> TryGetCandlesHistoryAsync(this ICandleshistoryservice service, string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await service.GetCandlesHistoryOrErrorAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, cancellationToken);

            return result as CandlesHistoryResponseModel;
        }

        // ReSharper disable once UnusedMember.Global
        public static async Task<CandlesHistoryResponseModel> GetCandlesHistoryAsync(this ICandleshistoryservice service, string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await service.GetCandlesHistoryOrErrorAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, cancellationToken);

            switch (result)
            {
                case CandlesHistoryResponseModel candlesHistoryResponseModel:
                    return candlesHistoryResponseModel;
                case ErrorResponse errorResponse:
                    throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result?.GetType()}");
        }

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
    }
}
