using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.DependencyInjection;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using Lykke.Service.CandlesHistory.Models;

namespace Lykke.Service.CandlesHistory
{
    public class Startup
    {
        public IHostingEnvironment HostingEnvironment { get; }
        public IContainer ApplicationContainer { get; private set; }
        public IConfigurationRoot Configuration { get; }
        public ILog Log { get; private set; }

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

            var builder = new ContainerBuilder();
            var settings = Configuration.LoadSettings<AppSettings>();
            var candlesHistory = settings.CurrentValue.CandlesHistory != null 
                ? settings.Nested(x => x.CandlesHistory)
                : settings.Nested(x => x.MtCandlesHistory);
            var candleHistoryAssetConnection = settings.CurrentValue.CandleHistoryAssetConnections != null 
                ? settings.Nested(x => x.CandleHistoryAssetConnections)
                : settings.Nested(x => x.MtCandleHistoryAssetConnections);

            Log = CreateLogWithSlack(services, settings);
            
            builder.RegisterModule(new ApiModule(
                candlesHistory, 
                settings.Nested(x => x.Assets),
                candleHistoryAssetConnection,
                Log));
            builder.Populate(services);
            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseLykkeMiddleware(nameof(Startup), ex => ErrorResponse.Create("Technical problem"));

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUi();
            app.UseStaticFiles();

            appLifetime.ApplicationStarted.Register(StartApplication);
            appLifetime.ApplicationStopping.Register(StopApplication);
            appLifetime.ApplicationStopped.Register(CleanUp);
        }

        private void StartApplication()
        {
            try
            {
                Console.WriteLine("Starting...");

                var startupManager = ApplicationContainer.Resolve<IStartupManager>();

                startupManager.StartAsync().Wait();

                Console.WriteLine("Started");
            }
            catch (Exception ex)
            {
                Log.WriteFatalErrorAsync(nameof(Startup), nameof(StartApplication), "", ex);
            }
        }

        private void StopApplication()
        {
            try
            {
                Console.WriteLine("Stopping...");

                var shutdownManager = ApplicationContainer.Resolve<IShutdownManager>();

                shutdownManager.ShutdownAsync().Wait();

                Console.WriteLine("Stopped");
            }
            catch (Exception ex)
            {
                Log.WriteFatalErrorAsync(nameof(Startup), nameof(StopApplication), "", ex);
            }
        }

        private void CleanUp()
        {
            try
            {
                Console.WriteLine("Cleaning up...");

                ApplicationContainer.Dispose();

                Console.WriteLine("Cleaned up");
            }
            catch (Exception ex)
            {
                Log.WriteFatalErrorAsync(nameof(Startup), nameof(CleanUp), "", ex);
            }
        }

        private static ILog CreateLogWithSlack(IServiceCollection services, IReloadingManager<AppSettings> settings)
        {
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();

            aggregateLogger.AddLog(consoleLogger);

            // Creating slack notification service, which logs own azure queue processing messages to aggregate log
            var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new AzureQueueIntegration.AzureQueueSettings
            {
                ConnectionString = settings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                QueueName = settings.CurrentValue.SlackNotifications.AzureQueue.QueueName
            }, aggregateLogger);

            var dbLogConnectionStringManager = settings.Nested(x => x.CandlesHistory.Db.LogsConnectionString);
            var dbLogConnectionString = dbLogConnectionStringManager.CurrentValue;

            // Creating azure storage logger, which logs own messages to concole log
            if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                const string appName = "Lykke.Service.CandlesHistory";

                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                    appName,
                    AzureTableStorage<LogEntity>.Create(dbLogConnectionStringManager, "CandlesHistoryServiceLogs", consoleLogger),
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
    }
}