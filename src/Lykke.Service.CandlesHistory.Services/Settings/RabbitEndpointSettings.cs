using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class RabbitEndpointSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }
        public string Namespace { get; set; }
    }
}
