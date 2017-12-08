using System;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Client.Custom;
using Lykke.Service.CandlesHistory.Client.Models;

// ReSharper disable once CheckNamespace
namespace Lykke.Service.CandlesHistory.Client
{
    public static partial class CandleshistoryserviceExtensions
    {
        public static async Task<CandlesHistoryResponseModel> TryGetCandlesHistoryAsync(this ICandleshistoryservice service, string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, System.DateTime fromMoment, System.DateTime toMoment, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await service.GetCandlesHistoryOrErrorAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, cancellationToken);

            return result as CandlesHistoryResponseModel;
        }

        public static async Task<CandlesHistoryResponseModel> GetCandlesHistoryAsync(this ICandleshistoryservice service, string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, System.DateTime fromMoment, System.DateTime toMoment, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await service.GetCandlesHistoryOrErrorAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, cancellationToken);

            if (result is CandlesHistoryResponseModel candlesHistoryResponseModel)
            {
                return candlesHistoryResponseModel;
            }

            if (result is ErrorResponse errorResponse)
            {
                throw new ErrorResponseException(errorResponse);
            }

            throw new InvalidOperationException($"Unexpected response type: {result?.GetType()}");
        }
    }
}
