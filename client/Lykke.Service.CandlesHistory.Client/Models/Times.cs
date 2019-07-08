// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Lykke.Service.CandlesHistory.Client.Models
{
    using Lykke.Service;
    using Lykke.Service.CandlesHistory;
    using Lykke.Service.CandlesHistory.Client;
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using System.Linq;

    public partial class Times
    {
        /// <summary>
        /// Initializes a new instance of the Times class.
        /// </summary>
        public Times()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the Times class.
        /// </summary>
        public Times(string totalPersistTime, string averagePersistTime, string lastPersistTime)
        {
            TotalPersistTime = totalPersistTime;
            AveragePersistTime = averagePersistTime;
            LastPersistTime = lastPersistTime;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "TotalPersistTime")]
        public string TotalPersistTime { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "AveragePersistTime")]
        public string AveragePersistTime { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "LastPersistTime")]
        public string LastPersistTime { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (TotalPersistTime == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "TotalPersistTime");
            }
            if (AveragePersistTime == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "AveragePersistTime");
            }
            if (LastPersistTime == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "LastPersistTime");
            }
        }
    }
}
