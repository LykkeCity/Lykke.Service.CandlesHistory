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

        public int PersistTasksQueueLength { get; set; }
        public int CandlesToPersistQueueLength { get; set; }
    }
}