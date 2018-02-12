using System;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    public class ErrorManagementSettings
    {
        /// <summary>
        /// The sign of whether we need to notify or not.
        /// </summary>
        public bool NotifyOnCantStoreAssetPair { get; set; }
        /// <summary>
        /// Log notification timeout in seconds.
        /// </summary>
        public TimeSpan NotifyOnCantStoreAssetPairTimeout { get; set; }
    }
}
