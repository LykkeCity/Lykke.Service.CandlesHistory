namespace Lykke.Service.CandlesHistory.Core.Services.HistoryMigration.HistoryProviders
{
    public interface IHistoryProvidersManager
    {
        IHistoryProvider GetProvider<TProvider>() where TProvider : IHistoryProvider;
    }
}
