namespace Lykke.Service.CandlesHistory.Models.IsAlive
{
    /// <summary>
    /// Checks service is alive response
    /// </summary>
    public class IsAliveResponse
    {
        /// <summary>
        /// API version
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Environment variables
        /// </summary>
        public string Env { get; set; }

        public int BatchesToPersistQueueLength { get; set; }
        public int CandlesToDispatchQueueLength { get; set; }
        public bool IsShuttingDown { get; set; }
        public bool IsShuttedDown { get; set; }
    }
}