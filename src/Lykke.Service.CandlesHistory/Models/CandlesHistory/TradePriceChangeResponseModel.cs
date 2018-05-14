using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.CandlesHistory.Models.CandlesHistory
{
    public class TradePriceChangeResponseModel
    {
        [Required]
        public decimal TradePriceChange { get; set; }

        public TradePriceChangeResponseModel(decimal priceChange)
        {
            TradePriceChange = priceChange;
        }
    }
}
