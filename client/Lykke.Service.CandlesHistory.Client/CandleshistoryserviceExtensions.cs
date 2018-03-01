﻿// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.CandlesHistory.Client
{
    using Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for Candleshistoryservice.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public static partial class CandleshistoryserviceExtensions
    {
            /// <summary>
            /// Pairs for which hisotry can be requested
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            // ReSharper disable once UnusedMember.Global
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
            // ReSharper disable once MemberCanBePrivate.Global
            public static async Task<IList<string>> GetAvailableAssetPairsAsync(this ICandleshistoryservice operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var result = await operations.GetAvailableAssetPairsWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='request'>
            /// </param>
            // ReSharper disable once UnusedMember.Global
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
            // ReSharper disable once MemberCanBePrivate.Global
            public static async Task<object> GetCandlesHistoryBatchOrErrorAsync(this ICandleshistoryservice operations, GetCandlesHistoryBatchRequest request = default(GetCandlesHistoryBatchRequest), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var result = await operations.GetCandlesHistoryBatchOrErrorWithHttpMessagesAsync(request, null, cancellationToken).ConfigureAwait(false))
                {
                    return result.Body;
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
            /// Price type. Possible values include: 'Unspecified', 'Bid', 'Ask', 'Mid'
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
            // ReSharper disable once UnusedMember.Global
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
            /// Price type. Possible values include: 'Unspecified', 'Bid', 'Ask', 'Mid'
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
            // ReSharper disable once MemberCanBePrivate.Global
            public static async Task<object> GetCandlesHistoryOrErrorAsync(this ICandleshistoryservice operations, string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, System.DateTime fromMoment, System.DateTime toMoment, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var result = await operations.GetCandlesHistoryOrErrorWithHttpMessagesAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, null, cancellationToken).ConfigureAwait(false))
                {
                    return result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='assetPair'>
            /// </param>
            // ReSharper disable once UnusedMember.Global
            public static void ApiCandlesHistoryMigrationByAssetPairPost(this ICandleshistoryservice operations, string assetPair)
            {
                operations.ApiCandlesHistoryMigrationByAssetPairPostAsync(assetPair).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='assetPair'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            // ReSharper disable once MemberCanBePrivate.Global
            public static async Task ApiCandlesHistoryMigrationByAssetPairPostAsync(this ICandleshistoryservice operations, string assetPair, CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiCandlesHistoryMigrationByAssetPairPostWithHttpMessagesAsync(assetPair, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            // ReSharper disable once UnusedMember.Global
            public static void ApiCandlesHistoryMigrationHealthGet(this ICandleshistoryservice operations)
            {
                operations.ApiCandlesHistoryMigrationHealthGetAsync().GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            // ReSharper disable once MemberCanBePrivate.Global
            public static async Task ApiCandlesHistoryMigrationHealthGetAsync(this ICandleshistoryservice operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiCandlesHistoryMigrationHealthGetWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='assetPair'>
            /// </param>
            // ReSharper disable once UnusedMember.Global
            public static void ApiCandlesHistoryMigrationHealthByAssetPairGet(this ICandleshistoryservice operations, string assetPair)
            {
                operations.ApiCandlesHistoryMigrationHealthByAssetPairGetAsync(assetPair).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='assetPair'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            // ReSharper disable once MemberCanBePrivate.Global
            public static async Task ApiCandlesHistoryMigrationHealthByAssetPairGetAsync(this ICandleshistoryservice operations, string assetPair, CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiCandlesHistoryMigrationHealthByAssetPairGetWithHttpMessagesAsync(assetPair, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <summary>
            /// Checks service is alive
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            // ReSharper disable once UnusedMember.Global
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
            // ReSharper disable once MemberCanBePrivate.Global
            public static async Task<IsAliveResponse> IsAliveAsync(this ICandleshistoryservice operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var result = await operations.IsAliveWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return result.Body;
                }
            }

    }
}
