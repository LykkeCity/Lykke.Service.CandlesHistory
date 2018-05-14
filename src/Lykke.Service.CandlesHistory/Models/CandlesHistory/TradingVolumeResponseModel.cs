using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.CandlesHistory.Models.CandlesHistory
{
    public class TradingVolumeResponseModel
    {
        [Required]
        public decimal Volume { get; set; }

        [Required]
        public decimal OppositeVolume { get; set; }

        public TradingVolumeResponseModel(decimal volume, decimal oppositeVolume)
        {
            Volume = volume;
            OppositeVolume = oppositeVolume;
        }

        public TradingVolumeResponseModel((decimal volume, decimal oppositeVolume) inpuTuple)
        {
            Volume = inpuTuple.volume;
            OppositeVolume = inpuTuple.oppositeVolume;
        }
    }
}
