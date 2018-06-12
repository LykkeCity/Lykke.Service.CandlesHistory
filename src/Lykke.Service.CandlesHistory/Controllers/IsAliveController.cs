using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.Common;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeAzureTable;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Logs.Loggers.LykkeSlack;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Models.IsAlive;
using Lykke.Service.CandlesHistory.Services.Settings;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Lykke.Service.CandlesHistory.Controllers
{
    /// <summary>
    /// Controller to test service is alive
    /// </summary>
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly IShutdownManager _shutdownManager;

        public IsAliveController(IShutdownManager shutdownManager)
        {
            _shutdownManager = shutdownManager;
        }

        /// <summary>
        /// Checks service is alive
        /// </summary>
        [HttpGet]
        [SwaggerOperation("IsAlive")]
        public IsAliveResponse Get()
        {
            using (var logFactory = LogFactory.Create()
                .AddConsole()
                .AddAzureTable(
                    _settings.Nested(s => s.Db.LogsConnectionString),
                    "CandlesHistoryServiceLogs2")
                .AddEssentialSlackChannels(
                    _appSettings.Nested(s => s.SlackNotifications.AzureQueue.ConnectionString).CurrentValue,
                    _appSettings.Nested(s => s.SlackNotifications.AzureQueue.QueueName).CurrentValue)
                .AddAdditionalSlackChannel(
                    _appSettings.Nested(s => s.SlackNotifications.AzureQueue.ConnectionString).CurrentValue,
                    _appSettings.Nested(s => s.SlackNotifications.AzureQueue.QueueName).CurrentValue,
                    "Prices"))
            {

                var log = logFactory.CreateLog("ProComponent1");

                log.Log(LogLevel.Error, 0, new LogEntryParameters(
                        "appName1",
                        "1.2.3",
                        "ENV_INFO1",
                        "?",
                        "process1",
                        1,
                        "message1",
                        new
                        {
                            a = 123,
                            b = "bbb"
                        },
                        null),
                    new Exception("exception1"),
                    (p, e) => p.Message);
            }

            return new IsAliveResponse
            {
                Name = AppEnvironment.Name,
                Version = AppEnvironment.Version,
                Env = AppEnvironment.EnvInfo,
                IsShuttingDown = _shutdownManager.IsShuttingDown,
                IsShuttedDown = _shutdownManager.IsShuttedDown
            };
        }
    }
}
