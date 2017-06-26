// ReSharper disable once CheckNamespace

using System;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Client.Models;

// ReSharper disable once CheckNamespace
namespace Lykke.Service.CandlesHistory.Client
{
    public static partial class CandleshistoryserviceExtensions
    {
        public static async Task<CandlesHistoryResponseModel> TryGetCandlesHistoryAsync(this ICandleshistoryservice service, string assetPairId, PriceType priceType, TimeInterval timeInterval, System.DateTime fromMoment, System.DateTime toMoment, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await service.GetCandlesHistoryOrErrorAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, cancellationToken);

            return result as CandlesHistoryResponseModel;
        }

        public static async Task<CandlesHistoryResponseModel> GetCandlesHistoryAsync(this ICandleshistoryservice service, string assetPairId, PriceType priceType, TimeInterval timeInterval, System.DateTime fromMoment, System.DateTime toMoment, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await service.GetCandlesHistoryOrErrorAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, cancellationToken);

            var candlesHistoryResponseModel = result as CandlesHistoryResponseModel;
            if (candlesHistoryResponseModel != null)
            {
                return candlesHistoryResponseModel;
            }

            var errorResponse = result as ErrorResponse;
            if (errorResponse != null)
            {
                throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result?.GetType()}");
        }
    }
}