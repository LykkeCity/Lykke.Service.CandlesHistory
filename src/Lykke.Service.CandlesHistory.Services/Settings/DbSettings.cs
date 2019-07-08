// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    [UsedImplicitly]
    public class DbSettings
    {
        public string LogsConnectionString { get; set; }
        public string SnapshotsConnectionString { get; set; }

        public StorageMode StorageMode { get; set; }
    }
}
