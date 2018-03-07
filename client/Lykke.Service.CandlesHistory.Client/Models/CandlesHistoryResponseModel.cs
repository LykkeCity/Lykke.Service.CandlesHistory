// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.CandlesHistory.Client.Models
{
    using Lykke.Service;
    using Lykke.Service.CandlesHistory;
    using Lykke.Service.CandlesHistory.Client;
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public partial class CandlesHistoryResponseModel
    {
        /// <summary>
        /// Initializes a new instance of the CandlesHistoryResponseModel
        /// class.
        /// </summary>
        public CandlesHistoryResponseModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the CandlesHistoryResponseModel
        /// class.
        /// </summary>
        public CandlesHistoryResponseModel(IList<Candle> history = default(IList<Candle>))
        {
            History = history;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "History")]
        public IList<Candle> History { get; set; }

    }
}
