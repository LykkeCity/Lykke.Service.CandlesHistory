using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Lykke.Service.CandlesHistory
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run();

            Console.WriteLine("Terminated");
        }
    }
}