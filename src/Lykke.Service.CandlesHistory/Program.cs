using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Candles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.CandlesHistory
{
    class Program
    {
        static void Main(string[] args)
        {
            var webHostCancellationTokenSource = new CancellationTokenSource();
            var end = new ManualResetEvent(false);

            AssemblyLoadContext.Default.Unloading += ctx =>
            {
                Console.WriteLine("SIGTERM recieved");

                webHostCancellationTokenSource.Cancel();

                end.WaitOne();
            };

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run(webHostCancellationTokenSource.Token);

            end.Set();

            Stop(host);

            Console.WriteLine("Terminated");
        }

        private static void Stop(IWebHost host)
        {
            var log = host.Services.GetService<ILog>();

            var broker = host.Services.GetService<CandlesBroker>();
            broker.Stop();

            var persistenceQueue = host.Services.GetService<ICandlesPersistenceQueue>();
            persistenceQueue.Persist();

            var queueLength = persistenceQueue.PersistTasksQueueLength;

            while (queueLength > 0)
            {
                log.WriteInfoAsync(nameof(Program), nameof(Stop), "", $"PersistenceQueue has {queueLength} tasks. Wait a second...");

                Task.Delay(TimeSpan.FromSeconds(1)).Wait();

                queueLength = persistenceQueue.PersistTasksQueueLength;
            }

            log.WriteInfoAsync(nameof(Program), nameof(Stop), "", "PersistenceQueue is empty");
        }
    }
}