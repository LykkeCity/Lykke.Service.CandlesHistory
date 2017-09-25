namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class RabbitSettings
    {
        public RabbitEndpointSettings CandlesSubscription { get; set; }
        public RabbitEndpointSettings FailedToPersistPublication { get; set; }
    }
}