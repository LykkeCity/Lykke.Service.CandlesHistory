namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class RabbitSettingsWithDeadLetter : RabbitSettings
    {
        public string DeadLetterExchangeName { get; set; }
    }
}