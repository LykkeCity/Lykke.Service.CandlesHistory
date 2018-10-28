using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.Logs.MsSql;
using Lykke.Logs.MsSql.Repositories;
using Lykke.Logs.Serilog;
using Lykke.Logs.Slack;
using Lykke.Service.CandlesHistory.Core.Domain;
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
using Lykke.Service.CandlesHistory.Services.Settings;
using AzureQueueSettings = Lykke.AzureQueueIntegration.AzureQueueSettings;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory
{
    [UsedImplicitly]
    public class Startup
    {
        private IContainer ApplicationContainer { get; set; }
        private IConfigurationRoot Configuration { get; }
        private ILog Log { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("env.json", optional: true)
                .AddSerilogJson(env)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddMvc()
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.Converters.Add(new StringEnumConverter());
                        options.SerializerSettings.ContractResolver =
                            new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    });

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration("v1", "Candles history service");
                });

                var builder = new ContainerBuilder();
                var settings = Configuration.LoadSettings<AppSettings>();
                var marketType = settings.CurrentValue.CandlesHistory != null
                    ? MarketType.Spot
                    : MarketType.Mt;

                var candlesHistory = settings.CurrentValue.CandlesHistory != null
                    ? settings.Nested(x => x.CandlesHistory)
                    : settings.Nested(x => x.MtCandlesHistory);

                Log = CreateLogWithSlack(Configuration, services, candlesHistory, 
                    settings.CurrentValue.SlackNotifications);
                
                builder.RegisterModule(new ApiModule(
                    marketType,
                    candlesHistory.CurrentValue,
                    settings.CurrentValue.Assets,
                    settings.CurrentValue.RedisSettings,
                    candlesHistory.ConnectionString(x => x.Db.SnapshotsConnectionString),
                    Log));
                builder.Populate(services);
                ApplicationContainer = builder.Build();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(ConfigureServices), "", ex).Wait();
                throw;
            }
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            try
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseLykkeMiddleware(nameof(Startup), ex => ErrorResponse.Create("Technical problem"));

                app.UseMvc();
                app.UseSwagger(c =>
                {
                    c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Host = httpReq.Host.Value);
                });
                app.UseSwaggerUI(x =>
                {
                    x.RoutePrefix = "swagger/ui";
                    x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                });
                app.UseStaticFiles();

                appLifetime.ApplicationStarted.Register(() => StartApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopping.Register(() => StopApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopped.Register(() => CleanUp().GetAwaiter().GetResult());
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(Configure), "", ex).Wait();
                throw;
            }
        }

        private async Task StartApplication()
        {
            try
            {
                await Log.WriteMonitorAsync("", "", "Started");
            }
            catch (Exception ex)
            {
                await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StartApplication), "", ex);
                throw;
            }
        }

        private async Task StopApplication()
        {
            try
            {
                await ApplicationContainer.Resolve<IShutdownManager>().ShutdownAsync();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StopApplication), "", ex);
                }
                throw;
            }
        }

        private async Task CleanUp()
        {
            try
            {
                if (Log != null)
                {
                    await Log.WriteMonitorAsync("", "", "Terminating");
                }

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(CleanUp), "", ex);
                }
                throw;
            }
        }

        private static ILog CreateLogWithSlack(IConfiguration configuration, IServiceCollection services, 
            IReloadingManager<CandlesHistorySettings> settings, SlackNotificationsSettings slackSettings)
        {
            const string tableName = "CandlesHistoryServiceLog";
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();
            var settingsValue = settings.CurrentValue;

            aggregateLogger.AddLog(consoleLogger);

            // Creating slack notification service, which logs own azure queue processing messages to aggregate log
            LykkeLogToAzureSlackNotificationsManager slackNotificationsManager = null;
            if (slackSettings?.AzureQueue?.ConnectionString != null && slackSettings.AzureQueue.QueueName != null)
            {
                // Creating slack notification service, which logs own azure queue processing messages to aggregate log
                var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new AzureQueueSettings
                {
                    ConnectionString = slackSettings.AzureQueue.ConnectionString,
                    QueueName = slackSettings.AzureQueue.QueueName
                }, aggregateLogger);

                slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(slackService, consoleLogger);

                var logToSlack = LykkeLogToSlack.Create(slackService, "Prices");

                aggregateLogger.AddLog(logToSlack);
            }

            if (settingsValue.UseSerilog)
            {
                aggregateLogger.AddLog(new SerilogLogger(typeof(Startup).Assembly, configuration));
            }
            else if (settingsValue.Db.StorageMode == StorageMode.SqlServer)
            {
                aggregateLogger.AddLog(
                    new LogToSql(new SqlLogRepository(tableName, settingsValue.Db.LogsConnectionString)));
            }
            else if (settingsValue.Db.StorageMode == StorageMode.Azure)
            {
                var dbLogConnectionString = settingsValue.Db.SnapshotsConnectionString;

                // Creating azure storage logger, which logs own messages to console log
                if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") 
                                                                      && dbLogConnectionString.EndsWith("}")))
                {
                    var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                        AzureTableStorage<Logs.LogEntity>.Create(settings.Nested(s => 
                            s.Db.SnapshotsConnectionString), tableName, consoleLogger),
                        consoleLogger);

                    var azureStorageLogger = new LykkeLogToAzureStorage(
                        persistenceManager,
                        slackNotificationsManager,
                        consoleLogger);

                    azureStorageLogger.Start();

                    aggregateLogger.AddLog(azureStorageLogger);
                }

            }


            return aggregateLogger;
        }
    }
}
