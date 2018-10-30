using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Sdk;

namespace Lykke.Service.CandlesHistory
{
    [UsedImplicitly]
    internal static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static async Task Main(string[] args)
        {
#if DEBUG
            await LykkeStarter.Start<Startup>(true);
#else
            await LykkeStarter.Start<Startup>(false);
#endif
        }
    }
}
