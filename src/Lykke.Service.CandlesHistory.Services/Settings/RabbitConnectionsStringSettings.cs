namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class RabbitConnectionsStringSettings
    {
        public string CandlesSubscription { get; set; }
        public string FailedToPersistPublication { get; set; }
    }
}