namespace Lykke.Service.CandlesHistory.Core.Services
{
    public interface IHaveState<TState>
    {
        TState GetState();
        void SetState(TState state);
    }
}