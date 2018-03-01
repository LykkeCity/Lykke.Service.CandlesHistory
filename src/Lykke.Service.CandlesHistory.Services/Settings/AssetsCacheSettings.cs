using System;
using JetBrains.Annotations;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    [UsedImplicitly]
    public class AssetsCacheSettings
    {
        public TimeSpan ExpirationPeriod { get; set; }
    }
}
