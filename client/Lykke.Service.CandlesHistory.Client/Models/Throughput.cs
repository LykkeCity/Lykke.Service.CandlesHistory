// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Lykke.Service.CandlesHistory.Client.Models
{
    using Lykke.Service;
    using Lykke.Service.CandlesHistory;
    using Lykke.Service.CandlesHistory.Client;
    using Newtonsoft.Json;
    using System.Linq;

    public partial class Throughput
    {
        /// <summary>
        /// Initializes a new instance of the Throughput class.
        /// </summary>
        public Throughput()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the Throughput class.
        /// </summary>
        public Throughput(int averageCandlesPersistedPerSecond, int averageCandleRowsPersistedPerSecond)
        {
            AverageCandlesPersistedPerSecond = averageCandlesPersistedPerSecond;
            AverageCandleRowsPersistedPerSecond = averageCandleRowsPersistedPerSecond;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "AverageCandlesPersistedPerSecond")]
        public int AverageCandlesPersistedPerSecond { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "AverageCandleRowsPersistedPerSecond")]
        public int AverageCandleRowsPersistedPerSecond { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            //Nothing to validate
        }
    }
}
