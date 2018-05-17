// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.CandlesHistory.Client.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class Candle
    {
        /// <summary>
        /// Initializes a new instance of the Candle class.
        /// </summary>
        public Candle()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the Candle class.
        /// </summary>
        public Candle(System.DateTime dateTime, double open, double close, double high, double low, double tradingVolume, double tradingOppositeVolume, double lastTradePrice)
        {
            DateTime = dateTime;
            Open = open;
            Close = close;
            High = high;
            Low = low;
            TradingVolume = tradingVolume;
            TradingOppositeVolume = tradingOppositeVolume;
            LastTradePrice = lastTradePrice;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "DateTime")]
        public System.DateTime DateTime { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Open")]
        public double Open { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Close")]
        public double Close { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "High")]
        public double High { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Low")]
        public double Low { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "TradingVolume")]
        public double TradingVolume { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "TradingOppositeVolume")]
        public double TradingOppositeVolume { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "LastTradePrice")]
        public double LastTradePrice { get; set; }

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
