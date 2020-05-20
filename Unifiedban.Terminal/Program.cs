/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Storage;
using Hangfire.Logging;
using System.Linq;
using Unifiedban.Models.Group;
using Unifiedban.Models.User;

namespace Unifiedban.Terminal
{
    class Program
    {
        //public static IConfigurationRoot Configuration;
        static BackgroundJobServer backgroundJobServer;

        static void Main(string[] args)
        {
            // Catch all unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // Load configuration from file
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
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

            LoadCacheData();
            InitializeHangfireServer();
            Controls.Manager.Initialize();
            Bot.MessageQueueManager.Initialize();
            Bot.CommandQueueManager.Initialize();
            Bot.Manager.Initialize(CacheData.Configuration["APIKEY"]);
            Utils.ConfigTools.Initialize();
            Utils.ChatTools.Initialize();
#if DEBUG
            TestArea.DoTest();
#endif

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
        private static void DisposeAll()
        {
            if (backgroundJobServer != null)
            {
                ClearHangfireJobs();
                backgroundJobServer.Dispose();
            }
            Bot.MessageQueueManager.Dispose();
            Bot.CommandQueueManager.Dispose();
            Bot.Manager.Dispose();
            Utils.ConfigTools.Dispose();
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
                CacheData.TrustFactors.Add(trustFactor.TelegramUserId, trustFactor);
            }

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

            CacheData.FatalError = true;
            Console.ReadLine();
            Environment.Exit(0);
        }


        #region " TEMP "
        //private static void checkBadWords(object sender, MessageEventArgs e)
        //{
        //    List<string> testCases = new List<string>();
        //    testCases.Add("(Mirko|Mirk0|Mirco|Mirc0|M1rko|M1rk0|M1rco|M1rc0)|(Culo|Cul0|Kulo|Kul0)");
        //    testCases.Add("(Gian|G1an|Gia4n|G14n)|(Culo|Cul0|Kulo|Kul0)");
        //    testCases.Add("(Gisan|G1an|Gidha4n|G14n)|(Culo|Cul0|K76gulo|Kul0)");
        //    testCases.Add("(Gidan|G1an|Gihfgha4n|G14n)|(Culo|Crtyrul0|Kulo|Kul0)");
        //    testCases.Add("(Gian|G1afdn|Gia4n|G14n)|(Culo|Culy0r|Kulo|6765)");
        //    testCases.Add("(Giafdn|G1345an|Gia4n|G14n)|(Culo|Culth0|Kulo|Kul0)");
        //    testCases.Add("(Giasn|G1an|Gia4fghn|G14n)|(Cutr67lo|Cul0|Kulo|Kurtyrtl0)");
        //    testCases.Add("(Gian|G15435an|Gia4n|G14n)|(C9ijulo|Cul0|Kfghfulo|Kul0)");
        //    testCases.Add("(56|Gfgh1an|Gia4n|G14n)|(Curtyrtuilo|Ckljklul0|Kughlo|Kul0)");
        //    testCases.Add("(Gituan|fghG1an|Gia4n|G14n)|(Cul6ro|Cul0|Kuryrlo|Kul0)");
        //    foreach (string test in testCases)
        //    {
        //        Regex reg = new Regex(test, RegexOptions.IgnoreCase);
        //        MatchCollection matchedWords = reg.Matches(e.Message.Text);
        //        if (matchedWords.Count > 1)
        //        {
        //            bool isKnown = spamValue.TryGetValue(e.Message.From.Id, out int vote);

        //            if (!isKnown)
        //                spamValue.Add(e.Message.From.Id, 0);

        //            spamValue[e.Message.From.Id] += 1;

        //            botClient.SendTextMessageAsync(
        //              chatId: e.Message.Chat,
        //              replyToMessageId: e.Message.MessageId,
        //              text: String.Format("New User @{0} spam vote is {1}", e.Message.From.Username, spamValue[e.Message.From.Id])
        //            );
        //        }
        //    }
        //}
        #endregion
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
