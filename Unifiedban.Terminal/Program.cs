/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Storage;
using Hangfire.Logging;
using System.Linq;
using System.Reflection;
using Quartz;
using Unifiedban.Models.Group;
using Unifiedban.Models.User;
using Unifiedban.Plugin.Common;

namespace Unifiedban.Terminal
{
    class Program
    {
        //public static IConfigurationRoot Configuration;
        static BackgroundJobServer? backgroundJobServer;
        private static bool _manualShutdown;
        internal static IScheduler? Scheduler;

        static void Main(string[] args)
        {
            // Catch all unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;

            // Load configuration from file
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            CacheData.Configuration = builder.Build();
            CacheData.LoggerName = CacheData.Configuration["LoggerName"];

            // Initialize logger
            if (!Directory.Exists(Environment.CurrentDirectory + "\\logs"))
                Directory.CreateDirectory(Environment.CurrentDirectory + "\\logs");
            Data.Utils.Logging logger = new Data.Utils.Logging();
            logger.Initialize(CacheData.LoggerName, Environment.CurrentDirectory);

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Warn,
                Message = "Initialization...",
                UserId = -1
            });

            // Initialize all managers
            InitializeAll();

            // The next line keeps the program running until the user presses ENTER (new line) on the keyboard
            Console.ReadLine();
            _manualShutdown = true;
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Warn,
                Message = "Shutdown started",
                UserId = -1
            });
            // Dispose all managers to avoid data loss
            DisposeAll();
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Warn,
                Message = "Shutdown completed.",
                UserId = -1
            });
            Environment.Exit(0);
        }

        private static void InitializeAll()
        {
            // Initialize database context
            using (Data.UBContext ubc = new Data.UBContext(
                CacheData.Configuration["Database"])) { }

            BusinessLogic.SysConfigLogic sysConfigLogic = new BusinessLogic.SysConfigLogic();
            CacheData.SysConfigs = new List<Models.SysConfig>(sysConfigLogic.Get());

            BusinessLogic.OperatorLogic operatorLogic = new BusinessLogic.OperatorLogic();
            CacheData.Operators = new List<Models.Operator>(operatorLogic.Get());

            Bot.Manager.Initialize(CacheData.Configuration["APIKEY"]);
            LoadCacheData();
            InitializeHangfireServer();
            InitQuartz();
            Controls.Manager.Initialize();
            Bot.MessageQueueManager.Initialize();
            Bot.CommandQueueManager.Initialize();
            Utils.LogTools.Initialize();
            Utils.ConfigTools.Initialize();
            Utils.ChatTools.Initialize();
            Utils.UserTools.Initialize();

            Bot.Manager.StartReceiving();

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Startup completed",
                UserId = -2
            });
        }
        internal static void DisposeAll()
        {
            CacheData.IsDisposing = true;
            Scheduler?.Shutdown();
            Bot.Manager.Dispose();
            if (backgroundJobServer != null)
            {
                ClearHangfireJobs();
                backgroundJobServer.Dispose();
            }
            Bot.MessageQueueManager.Dispose();
            Bot.CommandQueueManager.Dispose();
            Utils.ConfigTools.Dispose();
            Utils.UserTools.Dispose();
            Utils.LogTools.Dispose();
        }

        static void InitializeHangfireServer()
        {
            if (CacheData.FatalError)
                return;

            var options = new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = Convert.ToBoolean(CacheData.Configuration["HFPrepareSchema"]),
                QueuePollInterval = TimeSpan.FromSeconds(
                        Convert.ToInt32(CacheData.Configuration["HFPollingInterval"]))
            };

            GlobalConfiguration.Configuration.UseSqlServerStorage(CacheData.Configuration["HFDatabase"], options);
            GlobalConfiguration.Configuration.UseLogProvider(new HFLogProvider());
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 1 });

            backgroundJobServer = new BackgroundJobServer();
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "InitializeHangfireServer",
                Level = Models.SystemLog.Levels.Info,
                Message = "Hangifre server started",
                UserId = -1
            });

            ClearHangfireJobs();
        }
        static void ClearHangfireJobs()
        {

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal",
                Level = Models.SystemLog.Levels.Info,
                Message = "Hangifre clearing jobs (forced)",
                UserId = -1
            });
            using (var connection = JobStorage.Current.GetConnection())
            {
                foreach (var recurringJob in connection.GetRecurringJobs())
                {
                    RecurringJob.RemoveIfExists(recurringJob.Id);
                }
            }
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal",
                Level = Models.SystemLog.Levels.Info,
                Message = "Hangifre jobs cleared (forced)",
                UserId = -1
            });
        }
        public static bool InitializeTranslations()
        {
            try
            {
                CacheData.Translations = new Dictionary<string, Dictionary<string, Models.Translation.Entry>>();
                BusinessLogic.TranslationLogic translationLogic = new BusinessLogic.TranslationLogic();
                List<Models.Translation.Language> languages = translationLogic.GetLanguage();
                foreach(Models.Translation.Language language in languages)
                {
                    CacheData.Languages.TryAdd(language.LanguageId, language);
                    List<Models.Translation.Entry> entries = translationLogic
                        .GetEntriesByLanguage(language.LanguageId);
                    if (entries.Count == 0)
                        continue;

                    CacheData.Translations.TryAdd(language.LanguageId, new Dictionary<string, Models.Translation.Entry>());
                    foreach (Models.Translation.Entry entry in entries)
                        CacheData.Translations[language.LanguageId].TryAdd(entry.KeyId, entry);
                }

                return true;
            }
            catch (Exception ex)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Terminal.Program.InitializeTranslations",
                    Level = Models.SystemLog.Levels.Fatal,
                    Message = "Error loading translations.",
                    UserId = -1
                });

                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Terminal.Program.InitializeTranslations",
                    Level = Models.SystemLog.Levels.Fatal,
                    Message = ex.Message,
                    UserId = -1
                });
                if(ex.InnerException != null)
                    Data.Utils.Logging.AddLog(new Models.SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Unifiedban.Terminal.Program.InitializeTranslations",
                        Level = Models.SystemLog.Levels.Fatal,
                        Message = ex.InnerException.Message,
                        UserId = -1
                    });

                return false;
            }
        }
        public static void LoadCacheData()
        {
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Loading cache",
                UserId = -2
            });

            if (int.TryParse(CacheData.Configuration["CaptchaAutoKickTimer"], out int captchaAutoKickTimer))
            {
                CacheData.CaptchaAutoKickTimer = captchaAutoKickTimer;
            }

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Get translations",
                UserId = -2
            });

            CacheData.ControlChatId = Convert.ToInt64(CacheData.SysConfigs
                .Single(x => x.SysConfigId == "ControlChatId")
                .Value);

            if (!InitializeTranslations())
            {
                CacheData.FatalError = true;
                return;
            }

            LoadPlugins();

            LoadFiltersData();

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Get default group configuration parameters",
                UserId = -2
            });
            BusinessLogic.Group.ConfigurationParameterLogic configurationParameterLogic =
                new BusinessLogic.Group.ConfigurationParameterLogic();
            CacheData.GroupDefaultConfigs = 
                new List<Models.Group.ConfigurationParameter>(configurationParameterLogic.Get());

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Get registered groups",
                UserId = -2
            });
            BusinessLogic.Group.TelegramGroupLogic telegramGroupLogic =
                new BusinessLogic.Group.TelegramGroupLogic();
            foreach (Models.Group.TelegramGroup group in telegramGroupLogic.Get())
            {
                CacheData.Groups.Add(group.TelegramChatId, group);
                try
                {
                    CacheData.GroupConfigs.Add(
                        group.TelegramChatId,
                        JsonConvert
                            .DeserializeObject<
                                List<Models.Group.ConfigurationParameter>
                                >(group.Configuration));

                    AddMissingConfiguration(group.TelegramChatId);
                    /*
                     * To be used to enable messages to Group's Control Chat/Channel
                    Bot.MessageQueueManager.AddChatIfNotPresent(
                        Convert.ToInt64(CacheData.GroupConfigs[group.TelegramChatId]
                            .SingleOrDefault(x => x.ConfigurationParameterId == "ControlChatId").Value));
                            */
                }
                catch(Exception ex)
                {
                    Data.Utils.Logging.AddLog(new Models.SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Unifiedban Terminal Startup",
                        Level = Models.SystemLog.Levels.Error,
                        Message = $"Impossible to load group {group.TelegramChatId} " +
                            $"configuration:\n {ex.Message}",
                        UserId = -1
                    });
                }
                Bot.MessageQueueManager.AddGroupIfNotPresent(group);
            }

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Get banned users",
                UserId = -2
            });
            BusinessLogic.User.BannedLogic bannedLogic = new BusinessLogic.User.BannedLogic();
            CacheData.BannedUsers = new List<Models.User.Banned>(bannedLogic.Get());

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Get night schedule",
                UserId = -2
            });
            BusinessLogic.Group.NightScheduleLogic nsl = new BusinessLogic.Group.NightScheduleLogic();
            foreach(NightSchedule nightSchedule in nsl.Get())
            {
                CacheData.NightSchedules.Add(nightSchedule.GroupId, nightSchedule);
            }

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Get trust points",
                UserId = -2
            });
            BusinessLogic.User.TrustFactorLogic tfl = new BusinessLogic.User.TrustFactorLogic();
            foreach (TrustFactor trustFactor in tfl.Get())
            {
                if (!CacheData.TrustFactors.ContainsKey(trustFactor.TelegramUserId))
                {
                    CacheData.TrustFactors.Add(trustFactor.TelegramUserId, trustFactor);
                }
            }
#if DEBUG
            CacheData.BetaAuthChats.Add(-1001136742235);
            CacheData.BetaAuthChats.Add(-1001097564956);
            CacheData.BetaAuthChats.Add(-1001312334111);
            CacheData.BetaAuthChats.Add(-1001315904034);
            CacheData.BetaAuthChats.Add(-1001402005919);
            CacheData.BetaAuthChats.Add(-1001392951343);
            CacheData.BetaAuthChats.Add(-1001125553456);
            CacheData.BetaAuthChats.Add(-1001324395059);
            CacheData.BetaAuthChats.Add(-1001083786418);
            CacheData.BetaAuthChats.Add(-1001272088139);
            CacheData.BetaAuthChats.Add(-1001376622146);
            CacheData.BetaAuthChats.Add(-1001219480338);
#endif
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Cache loaded",
                UserId = -2
            });
        }
        static void LoadFiltersData()
        {
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Get filters data",
                UserId = -2
            });

            BusinessLogic.Filters.BadWordLogic badWordLogic = new BusinessLogic.Filters.BadWordLogic();
            CacheData.BadWords = badWordLogic.Get();
        }

        static void LoadPlugins()
        {
            string pluginsDir = Path.Combine(Environment.CurrentDirectory, "Plugins");
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup - LoadPlugins",
                Level = Models.SystemLog.Levels.Info,
                Message = $"Plugins directory: {pluginsDir}",
                UserId = -1
            });

            Directory.CreateDirectory(pluginsDir);
            string[] assemblies = Directory
                .GetFiles(pluginsDir, "Unifiedban.Plugin.*.dll");
            foreach (var file in assemblies)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban Terminal Startup - LoadPlugins",
                    Level = Models.SystemLog.Levels.Info,
                    Message = $"Trying to load Plugin {file} ...",
                    UserId = -1
                });

                try
                { 
                    Assembly assembly = Assembly.LoadFrom(file);
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.BaseType == typeof(UBPlugin))
                        {
                            object o = null;

                            ConstructorInfo? cWithAll = type.GetConstructor(new[] { typeof(Telegram.Bot.ITelegramBotClient),
                                    typeof(string), typeof(string), typeof(string) });
                            ConstructorInfo? cWithAllNoDb = type.GetConstructor(new[] { typeof(Telegram.Bot.ITelegramBotClient),
                                    typeof(string), typeof(string) });
                            ConstructorInfo? cWithBotClient = type.GetConstructor(new[] { typeof(Telegram.Bot.ITelegramBotClient) });
                            ConstructorInfo? cWithString = type.GetConstructor(new[] { typeof(string), typeof(string) });
                            if (cWithAll != null)
                            {
                                ParameterInfo parameter = cWithAll.GetParameters()
                                    .SingleOrDefault(x => x.Name == "databaseConnectionString");
                                if (parameter != null)
                                {
                                    parameter = cWithAll.GetParameters()
                                        .SingleOrDefault(x => x.Name == "hubServerAddress");
                                    if (parameter != null)
                                    {
                                        o = Activator.CreateInstance(type, Bot.Manager.BotClient, CacheData.Configuration["Database"],
                                            CacheData.Configuration["HubServerAddress"], CacheData.Configuration["HubServerToken"]);
                                    }
                                }
                            }
                            else if (cWithAll != null)
                            {
                                ParameterInfo parameter = cWithAll.GetParameters()
                                    .SingleOrDefault(x => x.Name == "hubServerAddress");
                                if (parameter != null)
                                {
                                    o = Activator.CreateInstance(type, Bot.Manager.BotClient,
                                        CacheData.Configuration["HubServerAddress"], CacheData.Configuration["HubServerToken"]);
                                }
                            }
                            else if (cWithBotClient != null)
                            {
                                o = Activator.CreateInstance(type, Bot.Manager.BotClient);
                            }
                            else if (cWithString != null)
                            {
                                o = Activator.CreateInstance(type, CacheData.Configuration["HubServerAddress"],
                                    CacheData.Configuration["HubServerToken"]);
                            }
                            else
                            {
                                o = Activator.CreateInstance(type);
                            }

                            UBPlugin plugin = (o as UBPlugin);
                            if (plugin == null)
                            {
                                Data.Utils.Logging.AddLog(new Models.SystemLog()
                                {
                                    LoggerName = CacheData.LoggerName,
                                    Date = DateTime.Now,
                                    Function = "Unifiedban Terminal Startup - LoadPlugins",
                                    Level = Models.SystemLog.Levels.Warn,
                                    Message = $"Error loading Plugin {file}",
                                    UserId = -1
                                });
                                continue;
                            }
                            switch (plugin.ExecutionPhase)
                            {
                                case IPlugin.Phase.PreCaptchaAndWelcome:
                                    CacheData.PreCaptchaAndWelcomePlugins.Add(plugin);
                                    break;
                                case IPlugin.Phase.PostCaptchaAndWelcome:
                                    CacheData.PostCaptchaAndWelcomePlugins.Add(plugin);
                                    break;
                                case IPlugin.Phase.PreControls:
                                    CacheData.PreControlsPlugins.Add(plugin);
                                    break;
                                case IPlugin.Phase.PostControls:
                                    CacheData.PostControlsPlugins.Add(plugin);
                                    break;
                                case IPlugin.Phase.PreFilters:
                                    CacheData.PreFiltersPlugins.Add(plugin);
                                    break;
                                case IPlugin.Phase.PostFilters:
                                    CacheData.PostFiltersPlugins.Add(plugin);
                                    break;
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Data.Utils.Logging.AddLog(new Models.SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Unifiedban Terminal Startup - LoadPlugins",
                        Level = Models.SystemLog.Levels.Error,
                        Message = ex.Message,
                        UserId = -1
                    });
                    if (ex.InnerException != null)
                    {
                        Data.Utils.Logging.AddLog(new Models.SystemLog()
                        {
                            LoggerName = CacheData.LoggerName,
                            Date = DateTime.Now,
                            Function = "Unifiedban Terminal Startup - LoadPlugins",
                            Level = Models.SystemLog.Levels.Error,
                            Message = ex.InnerException.Message,
                            UserId = -1
                        });
                    }
                }
            }
        }

        public static void AddMissingConfiguration(long telegramGroupId)
        {
            var diff = new List<ConfigurationParameter>();
            foreach(ConfigurationParameter configurationParameter in CacheData
                .GroupDefaultConfigs)
            {
                var exists = CacheData.GroupConfigs[telegramGroupId]
                    .SingleOrDefault(x => x.ConfigurationParameterId == configurationParameter.ConfigurationParameterId);
                if (exists == null)
                    diff.Add(configurationParameter);
            }

            if (diff.Count == 0)
                return;

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Warn,
                Message = $"Adding missing configuration to chat {telegramGroupId}",
                UserId = -2
            });

            foreach (ConfigurationParameter parameter in diff)
            {
                CacheData.GroupConfigs[telegramGroupId].Add(parameter);
            }

            BusinessLogic.Group.TelegramGroupLogic telegramGroupLogic =
                new BusinessLogic.Group.TelegramGroupLogic();
            telegramGroupLogic.UpdateConfiguration(
                telegramGroupId,
                JsonConvert.SerializeObject(CacheData.GroupConfigs[telegramGroupId]),
                -2);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (e.ExceptionObject as Exception);

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = "Unifiedban-Terminal",
                Date = DateTime.Now,
                Function = "Unifiedban Terminal",
                Level = Models.SystemLog.Levels.Fatal,
                Message = "UNHANDLED EXCEPTION:" + ex.Message,
                UserId = -1
            });

            if (ex.InnerException != null)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = "Unifiedban-Terminal",
                    Date = DateTime.Now,
                    Function = "Unifiedban Terminal",
                    Level = Models.SystemLog.Levels.Fatal,
                    Message = "UNHANDLED EXCEPTION (Inner):" + ex.InnerException.Message,
                    UserId = -1
                });
            }

            //CacheData.FatalError = true;
            Console.ReadLine();
            Environment.Exit(0);
        }
        
        private static void CurrentDomainOnProcessExit(object? sender, EventArgs e)
        {
            if (_manualShutdown) return;
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Warn,
                Message = "Auto shutdown started",
                UserId = -1
            });
            // Dispose all managers to avoid data loss
            DisposeAll();
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Warn,
                Message = "Shutdown completed.",
                UserId = -1
            });
            Environment.Exit(0);
        }

        private static async void InitQuartz()
        {
            var properties = new NameValueCollection();
            Scheduler = await SchedulerBuilder.Create(properties)
                .UseDefaultThreadPool(x => x.MaxConcurrency = 20)
                .UseInMemoryStore()
                .BuildScheduler();

            await Scheduler.Start();
            
            var uptimeJob = JobBuilder.Create<Jobs.UptimeJob>()
                .WithIdentity("UptimeJob", "uptime")
                .Build();
            var uptimeTrigger = TriggerBuilder.Create()
                .WithIdentity("uptimeTrigger", "uptime")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(int.Parse(CacheData.Configuration!["UptimeMonitor:Seconds"]))
                    .RepeatForever())
                .Build();
            await Scheduler.ScheduleJob(uptimeJob, uptimeTrigger);
        }
    }

    // Custom HF logger to avoid too many messages
    internal class HFLogger : Hangfire.Logging.ILog
    {
        public string Name { get; set; }

        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null)
        {
            if (messageFunc == null)
            {
                // Before calling a method with an actual message, LogLib first probes
                // whether the corresponding log level is enabled by passing a `null`
                // messageFunc instance.
                return logLevel > LogLevel.Debug;
            }

            Models.SystemLog.Levels obLogLevel = (Models.SystemLog.Levels)((int)logLevel - 1);

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "HFLogger.Log",
                Level = obLogLevel,
                Message = messageFunc(),
                UserId = -1
            });

            if (exception != null)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "HFLogger.Log",
                    Level = obLogLevel,
                    Message = exception.Message,
                    UserId = -1
                });

                if (exception.InnerException != null)
                    Data.Utils.Logging.AddLog(new Models.SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "HFLogger.Log",
                        Level = obLogLevel,
                        Message = exception.InnerException.Message,
                        UserId = -1
                    });
            }
            return true;
        }
    }
    internal class HFLogProvider : ILogProvider
    {
        public ILog GetLogger(string name)
        {
            // Logger name usually contains the full name of a type that uses it,
            // e.g. "Hangfire.Server.RecurringJobScheduler". It's used to know the
            // context of this or that message and for filtering purposes.
            return new HFLogger { Name = name };
        }
    }
}
