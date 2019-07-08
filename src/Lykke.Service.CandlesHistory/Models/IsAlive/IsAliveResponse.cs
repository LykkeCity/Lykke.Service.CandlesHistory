// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace Lykke.Service.CandlesHistory.Models.IsAlive
{
    /// <summary>
    /// Checks service is alive response
    /// </summary>
    public class IsAliveResponse
    {
        public string Name { get; set; }
        /// <summary>
        /// API version
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Environment variables
        /// </summary>
        public string Env { get; set; }
        public bool IsShuttingDown { get; set; }
        public bool IsShuttedDown { get; set; }
    }
}
