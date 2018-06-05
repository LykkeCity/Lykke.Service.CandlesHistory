using System;
using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    [UsedImplicitly]
    public class AssetsSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
        public TimeSpan AssetsCacheExpirationPeriod { get; set; }
        public TimeSpan AssetPairsCacheExpirationPeriod { get; set; }
    }
}
