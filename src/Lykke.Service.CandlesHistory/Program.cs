﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Lykke.Service.CandlesHistory
{
    internal static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static async Task Main(string[] args)
        {
            Console.WriteLine($"Lykke.Service.CandlesHistory version {AppEnvironment.Version}");
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif
            Console.WriteLine($"ENV_INFO: {AppEnvironment.EnvInfo}");

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine($"Unhandled exception: {e?.ExceptionObject}");

                if (e?.IsTerminating == true)
                {
                    Console.WriteLine("Terminating...");
                }
            };

            try
            {

                var host = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls("http://*:5000")
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .UseApplicationInsights()
                    .Build();

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error:");
                Console.WriteLine(ex);

                // Lets devops to see startup error in console between restarts in the Kubernetes
                var delay = TimeSpan.FromMinutes(1);

                Console.WriteLine();
                Console.WriteLine($"Process will be terminated in {delay}. Press any key to terminate immediately.");

                await Task.WhenAny(
                    Task.Delay(delay),
                    Task.Run(() => { Console.ReadKey(true); }));
            }

            Console.WriteLine("Terminated");
        }
    }
}
