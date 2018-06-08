using System;
using AsyncFriendlyStackTrace;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Service.CandlesHistory.DependencyInjection;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using Lykke.Service.CandlesHistory.Models;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;

namespace Lykke.Service.CandlesHistory
{
    [UsedImplicitly]
    public class Startup
    {
        private IContainer ApplicationContainer { get; set; }
        private IConfigurationRoot Configuration { get; }
        private ILog Log { get; set; }
        private IHealthNotifier HealthNotifier { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("env.json", optional: true)
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
                var candleHistoryAssetConnection = settings.CurrentValue.CandleHistoryAssetConnections != null
                    ? settings.Nested(x => x.CandleHistoryAssetConnections)
                    : settings.Nested(x => x.MtCandleHistoryAssetConnections);
               
                services.AddLykkeLogging(
                    candlesHistory.ConnectionString(x => x.Db.LogsConnectionString),
                    "CandlesHistoryServiceLogs",
                    settings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                    settings.CurrentValue.SlackNotifications.AzureQueue.QueueName,
                    logging =>
                    {
                        logging.AddAdditionalSlackChannel("Prices");

                        // Just for example:

                        logging.ConfigureAzureTable = options =>
                        {
                            options.BatchSizeThreshold = 1000;
                            options.MaxBatchLifetime = TimeSpan.FromSeconds(10);
                        };

                        logging.ConfigureConsole = options =>
                        {
                            options.IncludeScopes = true;
                        };

                        logging.ConfigureEssentialSlackChannels = options =>
                        {
                            options.SpamGuard.DisableGuarding();
                        };
                    });

                builder.Populate(services);

                builder.RegisterModule(new ApiModule(
                    marketType,
                    candlesHistory.CurrentValue,
                    settings.CurrentValue.Assets,
                    settings.CurrentValue.RedisSettings,
                    candleHistoryAssetConnection));
                
                ApplicationContainer = builder.Build();

                Log = ApplicationContainer.Resolve<ILogFactory>().CreateLog(this);

                HealthNotifier = ApplicationContainer.Resolve<IHealthNotifier>();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToAsyncString());
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

                app.UseLykkeMiddleware(ex => ErrorResponse.Create("Technical problem"));

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

                appLifetime.ApplicationStarted.Register(StartApplication);
                appLifetime.ApplicationStopping.Register(StopApplication);
                appLifetime.ApplicationStopped.Register(CleanUp);
            }
            catch (Exception ex)
            {
                Log.Critical(ex);
                throw;
            }
        }

        private void StartApplication()
        {
            try
            {
                HealthNotifier.Notify("Started");
            }
            catch (Exception ex)
            {
                Log.Critical(ex);
                throw;
            }
        }

        private void StopApplication()
        {
            try
            {
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }

        private void CleanUp()
        {
            try
            {
                HealthNotifier?.Notify("Terminating");
                ApplicationContainer?.Dispose();
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }
    }
}
