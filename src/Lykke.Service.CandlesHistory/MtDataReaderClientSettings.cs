using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory
{
    public class MtDataReaderClientSettings
    {
        [HttpCheck("/api/isalive")] 
        public string ServiceUrl { get; set; }
        public string ApiKey { get; set; }
    }
}
