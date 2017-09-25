using System;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.Service.CandlesHistory.Core;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.DependencyInjection;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Lykke.Service.CandlesHistory.Models;
using Lykke.Service.CandlesHistory.Services.Settings;
using AzureQueueSettings = Lykke.AzureQueueIntegration.AzureQueueSettings;

namespace Lykke.Service.CandlesHistory
{
    public class Startup
    {
        public IHostingEnvironment HostingEnvironment { get; }
        public IContainer ApplicationContainer { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            HostingEnvironment = env;
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                });

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", "Candles history service");
            });

            var settings = HttpSettingsLoader.Load<AppSettings>();
            
            var candlesHistory = settings.CandlesHistory ?? settings.MtCandlesHistory;      
            var candleHistoryAssetConnection = (settings.CandleHistoryAssetConnections ?? settings.MtCandleHistoryAssetConnections).ToDictionary(i => i.Key.ToUpperInvariant(), i => i.Value);

            var log = CreateLog(services, candlesHistory, settings.SlackNotifications);
            var builder = new ContainerBuilder();

            builder.RegisterModule(new ApiModule(candlesHistory, candleHistoryAssetConnection, settings.Assets, log));
            builder.Populate(services);

            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        private static ILog CreateLog(IServiceCollection services, CandlesHistorySettings candlesHistorySettings, SlackNotificationsSettings slackNotificationsSettings)
        {
            var appSettings = candlesHistorySettings;
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();

            aggregateLogger.AddLog(consoleLogger);

            var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new AzureQueueSettings
            {
                ConnectionString = slackNotificationsSettings.AzureQueue.ConnectionString,
                QueueName = slackNotificationsSettings.AzureQueue.QueueName
            }, aggregateLogger);

            if (!string.IsNullOrEmpty(appSettings.Db.LogsConnectionString) &&
                !(appSettings.Db.LogsConnectionString.StartsWith("${") && appSettings.Db.LogsConnectionString.EndsWith("}")))
            {
                const string appName = "Lykke.Service.CandlesHistory";

                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                    appName,
                    AzureTableStorage<LogEntity>.Create(() => appSettings.Db.LogsConnectionString, "CandlesHistoryServiceLogs", consoleLogger),
                    consoleLogger);

                var slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(appName, slackService, consoleLogger);

                var azureStorageLogger = new LykkeLogToAzureStorage(
                    appName,
                    persistenceManager,
                    slackNotificationsManager,
                    consoleLogger);

                azureStorageLogger.Start();

                aggregateLogger.AddLog(azureStorageLogger);
            }

            return aggregateLogger;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseLykkeMiddleware(nameof(Startup), ex => ErrorResponse.Create("Technical problem"));

            appLifetime.ApplicationStarted.Register(StartApplication);
            appLifetime.ApplicationStopping.Register(StopApplication);
            appLifetime.ApplicationStopped.Register(CleanUp);

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUi();
            app.UseStaticFiles();
        }

        private void StartApplication()
        {
            Console.WriteLine("Starting...");

            var startupManager = ApplicationContainer.Resolve<IStartupManager>();

            startupManager.StartAsync().Wait();

            Console.WriteLine("Started");
        }

        private void StopApplication()
        {
            Console.WriteLine("Stopping...");

            var shutdownManager = ApplicationContainer.Resolve<IShutdownManager>();

            shutdownManager.ShutdownAsync().Wait();

            Console.WriteLine("Stopped");
        }

        private void CleanUp()
        {
            Console.WriteLine("Cleaning up...");

            ApplicationContainer.Dispose();

            Console.WriteLine("Cleaned up");
        }
    }
}