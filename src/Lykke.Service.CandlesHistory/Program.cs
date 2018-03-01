﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Lykke.Service.CandlesHistory
{
    internal class Program
    {
        public static string EnvInfo => Environment.GetEnvironmentVariable("ENV_INFO");

        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            Console.WriteLine($"Lykke.Service.CandlesHistory version {Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion}");
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif
            Console.WriteLine($"ENV_INFO: {EnvInfo}");

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

                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error:");
                Console.WriteLine(ex);

                // Lets devops to see startup error in console between restarts in the Kubernetes
                var delay = TimeSpan.FromMinutes(1);

                Console.WriteLine();
                Console.WriteLine($"Process will be terminated in {delay}. Press any key to terminate immediately.");

                Task.WhenAny(
                        Task.Delay(delay),
                        Task.Run(() =>
                        {
                            Console.ReadKey(true);
                        }))
                    .GetAwaiter()
                    .GetResult();
            }

            Console.WriteLine("Terminated");
        }
    }
}
