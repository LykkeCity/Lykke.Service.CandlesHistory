using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using Lykke.Service.CandlesHistory.Core.Services;
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

            Stop(host);

            Console.WriteLine("Terminated");

            end.Set();
        }

        private static void Stop(IWebHost host)
        {
            Console.WriteLine("Stopping...");

            var shutdownManager = host.Services.GetService<IShutdownManager>();

            shutdownManager.Shutdown().Wait();

            Console.WriteLine("Stopped");
        }
    }
}