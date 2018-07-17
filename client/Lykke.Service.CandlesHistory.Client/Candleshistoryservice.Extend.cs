using System;
using System.Net.Http;

namespace Lykke.Service.CandlesHistory.Client
{
    public partial class Candleshistoryservice
    {
        /// <inheritdoc />
        /// <summary>
        /// Should be used to prevent memory leak in RetryPolicy
        /// </summary>
        public Candleshistoryservice(Uri baseUri, HttpClient client) : base(client)
        {
            Initialize();

            BaseUri = baseUri ?? throw new ArgumentNullException("baseUri");
        }
    }
}
