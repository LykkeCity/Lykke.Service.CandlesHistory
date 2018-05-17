using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.CandlesHistory.Models.CandlesHistory
{
    public class LastTradePriceResponseModel
    {
        [Required]
        public decimal LastTradePrice { get; set; }

        public LastTradePriceResponseModel(decimal price)
        {
            LastTradePrice = price;
        }
    }
}
