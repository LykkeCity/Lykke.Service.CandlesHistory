// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.CandlesHistory.Client
{
    using Lykke.Service;
    using Lykke.Service.CandlesHistory;
    using Models;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for Candleshistoryservice.
    /// </summary>
    public static partial class CandleshistoryserviceExtensions
    {
            /// <summary>
            /// Pairs for which hisotry can be requested
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            public static IList<string> GetAvailableAssetPairs(this ICandleshistoryservice operations)
            {
                return operations.GetAvailableAssetPairsAsync().GetAwaiter().GetResult();
            }

            /// <summary>
            /// Pairs for which hisotry can be requested
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IList<string>> GetAvailableAssetPairsAsync(this ICandleshistoryservice operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.GetAvailableAssetPairsWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='request'>
            /// </param>
            public static object GetCandlesHistoryBatchOrError(this ICandleshistoryservice operations, GetCandlesHistoryBatchRequest request = default(GetCandlesHistoryBatchRequest))
            {
                return operations.GetCandlesHistoryBatchOrErrorAsync(request).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='request'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<object> GetCandlesHistoryBatchOrErrorAsync(this ICandleshistoryservice operations, GetCandlesHistoryBatchRequest request = default(GetCandlesHistoryBatchRequest), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.GetCandlesHistoryBatchOrErrorWithHttpMessagesAsync(request, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <summary>
            /// Asset's candles history
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='assetPairId'>
            /// Asset pair ID
            /// </param>
            /// <param name='priceType'>
            /// Price type. Possible values include: 'Unspecified', 'Bid', 'Ask', 'Mid',
            /// 'Trades'
            /// </param>
            /// <param name='timeInterval'>
            /// Time interval. Possible values include: 'Unspecified', 'Sec', 'Minute',
            /// 'Min5', 'Min15', 'Min30', 'Hour', 'Hour4', 'Hour6', 'Hour12', 'Day',
            /// 'Week', 'Month'
            /// </param>
            /// <param name='fromMoment'>
            /// From moment in ISO 8601 (inclusive)
            /// </param>
            /// <param name='toMoment'>
            /// To moment in ISO 8601 (exclusive)
            /// </param>
            public static object GetCandlesHistoryOrError(this ICandleshistoryservice operations, string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, System.DateTime fromMoment, System.DateTime toMoment)
            {
                return operations.GetCandlesHistoryOrErrorAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment).GetAwaiter().GetResult();
            }

            /// <summary>
            /// Asset's candles history
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='assetPairId'>
            /// Asset pair ID
            /// </param>
            /// <param name='priceType'>
            /// Price type. Possible values include: 'Unspecified', 'Bid', 'Ask', 'Mid',
            /// 'Trades'
            /// </param>
            /// <param name='timeInterval'>
            /// Time interval. Possible values include: 'Unspecified', 'Sec', 'Minute',
            /// 'Min5', 'Min15', 'Min30', 'Hour', 'Hour4', 'Hour6', 'Hour12', 'Day',
            /// 'Week', 'Month'
            /// </param>
            /// <param name='fromMoment'>
            /// From moment in ISO 8601 (inclusive)
            /// </param>
            /// <param name='toMoment'>
            /// To moment in ISO 8601 (exclusive)
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<object> GetCandlesHistoryOrErrorAsync(this ICandleshistoryservice operations, string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, System.DateTime fromMoment, System.DateTime toMoment, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.GetCandlesHistoryOrErrorWithHttpMessagesAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <summary>
            /// Checks service is alive
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            public static IsAliveResponse IsAlive(this ICandleshistoryservice operations)
            {
                return operations.IsAliveAsync().GetAwaiter().GetResult();
            }

            /// <summary>
            /// Checks service is alive
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IsAliveResponse> IsAliveAsync(this ICandleshistoryservice operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.IsAliveWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

    }
}
