using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Sdk.Settings;
using Lykke.Service.CandlesHistory.Services.Settings;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        [Optional]
        public CandlesHistorySettings CandlesHistory { get; set; }
        [Optional]
        public CandlesHistorySettings MtCandlesHistory { get; set; }

        [Optional]
        public Dictionary<string, string> CandleHistoryAssetConnections { get; set; }

        [Optional]
        public Dictionary<string, string> MtCandleHistoryAssetConnections { get; set; }

        public AssetsSettings Assets { get; set; }

        public RedisSettings RedisSettings { get; set; }
    }
}
