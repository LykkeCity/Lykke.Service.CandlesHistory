using Autofac;
using Common;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesSubscriber : IStartable, IStopable
    {
    }
}