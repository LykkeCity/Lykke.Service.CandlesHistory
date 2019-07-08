// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Lykke.Service.CandlesHistory.Client.Models
{
    using Lykke.Service;
    using Lykke.Service.CandlesHistory;
    using Lykke.Service.CandlesHistory.Client;
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Checks service is alive response
    /// </summary>
    public partial class IsAliveResponse
    {
        /// <summary>
        /// Initializes a new instance of the IsAliveResponse class.
        /// </summary>
        public IsAliveResponse()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the IsAliveResponse class.
        /// </summary>
        /// <param name="version">API version</param>
        /// <param name="env">Environment variables</param>
        public IsAliveResponse(bool isShuttingDown, bool isShuttedDown, string name = default(string), string version = default(string), string env = default(string), PersistenceInfo persistence = default(PersistenceInfo))
        {
            Name = name;
            Version = version;
            Env = env;
            IsShuttingDown = isShuttingDown;
            IsShuttedDown = isShuttedDown;
            Persistence = persistence;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets API version
        /// </summary>
        [JsonProperty(PropertyName = "Version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets environment variables
        /// </summary>
        [JsonProperty(PropertyName = "Env")]
        public string Env { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "IsShuttingDown")]
        public bool IsShuttingDown { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "IsShuttedDown")]
        public bool IsShuttedDown { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Persistence")]
        public PersistenceInfo Persistence { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (Persistence != null)
            {
                Persistence.Validate();
            }
        }
    }
}
