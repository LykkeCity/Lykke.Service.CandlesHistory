// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Lykke.Service.CandlesHistory.Client
{
    using Lykke.Service;
    using Lykke.Service.CandlesHistory;
    using Microsoft.Rest;
    using Models;
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// </summary>
    public partial interface ICandleshistoryservice : System.IDisposable
    {
        /// <summary>
        /// The base URI of the service.
        /// </summary>
        System.Uri BaseUri { get; set; }

        /// <summary>
        /// Gets or sets json serialization settings.
        /// </summary>
        JsonSerializerSettings SerializationSettings { get; }

        /// <summary>
        /// Gets or sets json deserialization settings.
        /// </summary>
        JsonSerializerSettings DeserializationSettings { get; }


        /// <summary>
        /// Pairs for which hisotry can be requested
        /// </summary>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<IList<string>>> GetAvailableAssetPairsWithHttpMessagesAsync(Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <param name='request'>
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> GetCandlesHistoryBatchOrErrorWithHttpMessagesAsync(GetCandlesHistoryBatchRequest request = default(GetCandlesHistoryBatchRequest), Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asset's candles history
        /// </summary>
        /// <param name='assetPairId'>
        /// Asset pair ID
        /// </param>
        /// <param name='priceType'>
        /// Price type. Possible values include: 'Unspecified', 'Bid', 'Ask',
        /// 'Mid', 'Trades'
        /// </param>
        /// <param name='timeInterval'>
        /// Time interval. Possible values include: 'Unspecified', 'Sec',
        /// 'Minute', 'Min5', 'Min15', 'Min30', 'Hour', 'Hour4', 'Hour6',
        /// 'Hour12', 'Day', 'Week', 'Month'
        /// </param>
        /// <param name='fromMoment'>
        /// From moment in ISO 8601 (inclusive)
        /// </param>
        /// <param name='toMoment'>
        /// To moment in ISO 8601 (exclusive)
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> GetCandlesHistoryOrErrorWithHttpMessagesAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, System.DateTime fromMoment, System.DateTime toMoment, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the time of the closest available bar in the past if any.
        /// </summary>
        /// <param name='assetPairId'>
        /// Asset pair ID
        /// </param>
        /// <param name='priceType'>
        /// Price type. Possible values include: 'Unspecified', 'Bid', 'Ask',
        /// 'Mid', 'Trades'
        /// </param>
        /// <param name='timeInterval'>
        /// Time interval. Possible values include: 'Unspecified', 'Sec',
        /// 'Minute', 'Min5', 'Min15', 'Min30', 'Hour', 'Hour4', 'Hour6',
        /// 'Hour12', 'Day', 'Week', 'Month'
        /// </param>
        /// <param name='lastMoment'>
        /// From moment in ISO 8601
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> GetRecentCandleTimeOrErrorWithHttpMessagesAsync(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, System.DateTime lastMoment, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Checks service is alive
        /// </summary>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<IsAliveResponse>> IsAliveWithHttpMessagesAsync(Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));

    }
}
