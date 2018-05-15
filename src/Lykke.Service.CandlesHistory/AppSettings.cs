using System.Collections.Generic;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Services.Settings;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory
{
    public class AppSettings
    {
        [Optional]
        public CandlesHistorySettings CandlesHistory { get; set; }
        [Optional]
        public CandlesHistorySettings MtCandlesHistory { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }

        [Optional]
        public Dictionary<string, string> CandleHistoryAssetConnections { get; set; }

        [Optional]
        public Dictionary<string, string> MtCandleHistoryAssetConnections { get; set; }

        public AssetsSettings Assets { get; set; }

        public RedisSettings RedisSettings { get; set; }
    }
}
