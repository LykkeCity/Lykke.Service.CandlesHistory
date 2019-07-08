// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
