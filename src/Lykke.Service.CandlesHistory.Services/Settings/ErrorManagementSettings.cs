using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class ErrorManagementSettings
    {
        [AmqpCheck]
        public bool ExceptionOnCantStoreAssetPair { get; set; }
    }
}
