// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Lykke.Service.CandlesHistory.Client.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class RecentCandleTimeResponseModel
    {
        /// <summary>
        /// Initializes a new instance of the RecentCandleTimeResponseModel
        /// class.
        /// </summary>
        public RecentCandleTimeResponseModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the RecentCandleTimeResponseModel
        /// class.
        /// </summary>
        public RecentCandleTimeResponseModel(bool exists, System.DateTime resultTimestamp)
        {
            Exists = exists;
            ResultTimestamp = resultTimestamp;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Exists")]
        public bool Exists { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ResultTimestamp")]
        public System.DateTime ResultTimestamp { get; set; }

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
