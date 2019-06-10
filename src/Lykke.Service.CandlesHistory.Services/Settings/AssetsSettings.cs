using System;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class AssetsSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
        
        public TimeSpan CacheExpirationPeriod { get; set; }
        
        [Optional]
        public string ApiKey { get; set; }
    }
}
