 /* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Hangfire;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Unifiedban.Models.Group;
using Timer = System.Timers.Timer;

 namespace Unifiedban.Terminal.Utils
{
    public class ConfigTools
    {
        static readonly BusinessLogic.Group.TelegramGroupLogic telegramGroupLogic =
            new BusinessLogic.Group.TelegramGroupLogic();
        static readonly BusinessLogic.Group.NightScheduleLogic nsl =
            new BusinessLogic.Group.NightScheduleLogic();
        
        static Timer _keepWsAlive;
        static HubConnection _connection;
        
        private static IModel _channel;
        private static IBasicProperties _properties;

        public static void Initialize()
        {
            RecurringJob.AddOrUpdate("ConfigTools_SyncGroupsConfigToDatabase", () => SyncGroupsConfigToDatabase(), "0/30 * * ? * *");
            RecurringJob.AddOrUpdate("ConfigTools_SyncWelcomeAndRulesText", () => SyncWelcomeAndRulesText(), "0/30 * * ? * *");
            RecurringJob.AddOrUpdate("ConfigTools_SyncGroupsToDatabase", () => SyncGroupsToDatabase(), "0/30 * * ? * *");
            RecurringJob.AddOrUpdate("ConfigTools_SyncNightScheduleToDatabase", () => SyncNightScheduleToDatabase(), "0/30 * * ? * *");

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Config Tools initialized",
                UserId = -2
            });
            
            _keepWsAlive = new Timer(1000 * 15);
            _keepWsAlive.Elapsed += KeepWSAliveOnElapsed;
            _keepWsAlive.AutoReset = true;

            ConnectToHub();

            LoadRabbitMQManager();
        }

        private static void KeepWSAliveOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (_connection.State != HubConnectionState.Connected)
            {
                _keepWsAlive.Stop();
                return;
            }
            _connection.InvokeAsync("Echo", "Ping!");
#if DEBUG
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "KeepWSAliveOnElapsed",
                Level = Models.SystemLog.Levels.Debug,
                Message = "Ping!",
                UserId = -1
            });
#endif
        }

        public static void Dispose()
        {
            SyncGroupsToDatabase();
            SyncGroupsConfigToDatabase();
            SyncWelcomeAndRulesText();
            SyncNightScheduleToDatabase();
            
            _keepWsAlive.Stop();
            if (_connection.State == HubConnectionState.Connected)
            {
                _connection.StopAsync().Wait();
            }
            
            if(_channel.IsOpen) _channel.Close();
        }

        public static void SyncGroupsToDatabase()
        {
            foreach (long group in CacheData.Groups.Keys)
                telegramGroupLogic.Update(
                    CacheData.Groups[group],
                    -2);
        }

        public static void SyncGroupsConfigToDatabase()
        {
            foreach (long group in CacheData.GroupConfigs.Keys)
                telegramGroupLogic.UpdateConfiguration(
                    group,
                    JsonConvert.SerializeObject(CacheData.GroupConfigs[group]),
                    -2);
        }

        public static void SyncWelcomeAndRulesText()
        {
            foreach (long group in CacheData.Groups.Keys)
            {
                telegramGroupLogic.UpdateWelcomeText(
                    group, CacheData.Groups[group].WelcomeText,
                    -2);
                telegramGroupLogic.UpdateRulesText(
                    group, CacheData.Groups[group].RulesText,
                    -2);
            }
        }

        public static bool UpdateWelcomeText(long groupId, string text)
        {
            try
            {
                CacheData.Groups[groupId].WelcomeText = text;
                
                Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.Groups[groupId].TelegramChatId,
                    parseMode: ParseMode.Markdown,
                    text: CacheData.GetTranslation(
                        CacheData.Groups[groupId].SettingsLanguage,
                        "command_setwelcome_success"),
                    replyMarkup: new ReplyKeyboardRemove() { Selective = true }
                );
                return true;
            }
            catch
            {
                Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.Groups[groupId].TelegramChatId,
                    parseMode: ParseMode.Markdown,
                    text: CacheData.GetTranslation(
                        CacheData.Groups[groupId].SettingsLanguage,
                        "command_setwelcome_error"),
                    replyMarkup: new ReplyKeyboardRemove() { Selective = true }
                );
                return false;
            }
        }
        
        public static bool UpdateRulesText(long groupId, string text)
        {
            try
            {
                CacheData.Groups[groupId].RulesText = text;
                
                Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.Groups[groupId].TelegramChatId,
                    parseMode: ParseMode.Markdown,
                    text: CacheData.GetTranslation(
                        CacheData.Groups[groupId].SettingsLanguage,
                        "command_setrules_success"),
                    replyMarkup: new ReplyKeyboardRemove() { Selective = true }
                );
                return true;
            }
            catch
            {
                Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.Groups[groupId].TelegramChatId,
                    parseMode: ParseMode.Markdown,
                    text: CacheData.GetTranslation(
                        CacheData.Groups[groupId].SettingsLanguage,
                        "command_setrules_error"),
                    replyMarkup: new ReplyKeyboardRemove() { Selective = true }
                );
                return false;
            }
        }

        public static void SyncNightScheduleToDatabase()
        {
            foreach (Models.Group.NightSchedule nightSchedule in CacheData.NightSchedules.Values)
            {
                nsl.Update(nightSchedule, -2);
            }
        }

        private static void ConnectToHub()
        {
            if (CacheData.FatalError ||
                CacheData.IsDisposing)
            {
                return;
            }
            
            _connection = new HubConnectionBuilder()
                .WithUrl(CacheData.Configuration["HubServerAddress"])
                .WithAutomaticReconnect()
                .Build();

            _connection.Closed += exception =>
            {
                _keepWsAlive.Stop();
                if (CacheData.FatalError ||
                    CacheData.IsDisposing)
                {
                    return Task.CompletedTask;
                }

                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "connectToHub()",
                    Level = Models.SystemLog.Levels.Info,
                    Message = "Connection to Hub lost.",
                    UserId = -1
                });

                Thread.Sleep(2000);
                ConnectToHub();
                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "connectToHub()",
                    Level = Models.SystemLog.Levels.Info,
                    Message = "Reconnecting Hub Server",
                    UserId = -1
                });

                _connection.InvokeAsync("Identification", CacheData.Configuration["HubServerToken"]);
                _keepWsAlive.Start();

                return Task.CompletedTask;
            };

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "connectToHub()",
                Level = Models.SystemLog.Levels.Info,
                Message = "Connecting and autenticating to Hub Server",
                UserId = -1
            });
            _connection.StartAsync().Wait();
            _connection.InvokeAsync("Identification", CacheData.Configuration["HubServerToken"]);

            _connection.On<string, string>("MessageToBot",
                (functionName, data) =>
                {
                    switch (functionName)
                    {
                        case "Identification":
                            if (data == "KO")
                            {
                                Data.Utils.Logging.AddLog(new Models.SystemLog()
                                {
                                    LoggerName = CacheData.LoggerName,
                                    Date = DateTime.Now,
                                    Function = "MessageToBot",
                                    Level = Models.SystemLog.Levels.Error,
                                    Message = "Hub Server answered KO on authentication",
                                    UserId = -1
                                });
                                _connection.DisposeAsync();
                            }
                            else
                            {
                                Data.Utils.Logging.AddLog(new Models.SystemLog()
                                {
                                    LoggerName = CacheData.LoggerName,
                                    Date = DateTime.Now,
                                    Function = "MessageToBot",
                                    Level = Models.SystemLog.Levels.Info,
                                    Message = "Hub Server answered OK on authentication",
                                    UserId = -1
                                });
                                _keepWsAlive.Start();
                            }
                            break;
                    }
                });

            _connection.On<string, string, string, string>("UpdateSetting", 
                (dashboardUserId, groupId, parameter, value) =>
            {
                if (CacheData.FatalError ||
                    CacheData.IsDisposing)
                {
                    return;
                }

                HandleWsCommand(dashboardUserId, groupId, parameter, value);
            });
            
            _connection.On<string, string, string, bool>("ToggleStatus", 
                (dashboardUserId, groupId, parameter, value) =>
                {
                    if (CacheData.FatalError ||
                        CacheData.IsDisposing)
                    {
                        return;
                    }

                    TelegramGroup group = CacheData.Groups.Values
                        .SingleOrDefault(x => x.GroupId == groupId);
                    if (group == null)
                    {
                        return;
                    }

                    if (!UserTools.CanHandleGroup(dashboardUserId, groupId))
                    {
                        return;
                    }

                    group.State = value ? TelegramGroup.Status.Active : TelegramGroup.Status.Inactive;
                });
        }

        private static void HandleWsCommand(string dashboardUserId, string groupId, string parameter, string value)
        {
            var group = CacheData.Groups.Values
                .SingleOrDefault(x => x.GroupId == groupId);
            if (group == null)
            {
                return;
            }

            if (!UserTools.CanHandleGroup(dashboardUserId, groupId))
            {
                return;
            }

            switch (parameter)
            {
                case "WelcomeText":
                    CacheData.Groups[group.TelegramChatId].WelcomeText = value;
                    return;
                case "RulesText":
                    CacheData.Groups[group.TelegramChatId].RulesText = value;
                    return;
                case "ReportChatId":
                {
                    if (!long.TryParse(value, out var reportChatId))
                    {
                        return;
                    }
                    CacheData.Groups[group.TelegramChatId].ReportChatId = reportChatId;
                    return;
                }
                case "SettingsLanguage":
                    CacheData.Groups[group.TelegramChatId].SettingsLanguage = value;
                    return;
                case "InviteAlias":
                    CacheData.Groups[group.TelegramChatId].InviteAlias = value;
                    return;
                case "Gate":
                    Bot.Command.Gate.ToggleGate(new Message()
                    {
                        Chat = new Chat()
                        {
                            Id = group.TelegramChatId
                        }
                    }, value.ToLower() == "true");
                    return;
            }

            var config = CacheData.GroupConfigs[group.TelegramChatId]
                .SingleOrDefault(x => x.ConfigurationParameterId == parameter);
            if (config == null)
                return;

            CacheData.GroupConfigs[group.TelegramChatId]
                [CacheData.GroupConfigs[group.TelegramChatId]
                    .IndexOf(config)]
                .Value = value;
        }
        
        
        static void LoadRabbitMQManager()
        {
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "ConfigTools.LoadRabbitMQManager",
                Level = Models.SystemLog.Levels.Info,
                Message = "Creating RabbitMQ instance...",
                UserId = -1
            });
            var factory = new ConnectionFactory();
            factory.UserName = "";
            factory.Password = "";
            factory.VirtualHost = "/";
            factory.HostName = "";
            factory.Port = 0000;
            factory.DispatchConsumersAsync = true;
            
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "ConfigTools.LoadRabbitMQManager",
                Level = Models.SystemLog.Levels.Info,
                Message = "Connecting to RabbitMQ server...",
                UserId = -1
            });
            var conn = factory.CreateConnection();
            _channel = conn.CreateModel();
            
            _properties = _channel.CreateBasicProperties();
            
            /*
            var tgConsumer = new AsyncEventingBasicConsumer(_channel);
            tgConsumer.Received += ConsumerOnTgMessage;
            
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "ConfigTools.LoadRabbitMQManager",
                Level = Models.SystemLog.Levels.Info,
                Message = "Start consuming ub3.commands queue...",
                UserId = -1
            });
            _channel.BasicConsume("ub3.commands", false, tgConsumer);
            */
            
        }
        
        private static async Task ConsumerOnTgMessage(object sender, BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            var str = System.Text.Encoding.Default.GetString(body);

            if (!str.StartsWith("UB3|"))
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "ConfigTools.LoadRabbitMQManager",
                    Level = Models.SystemLog.Levels.Warn,
                    Message = "Malformed message received:\n" +
                              $"{str}",
                    UserId = -1
                });
                
                _channel.BasicAck(ea.DeliveryTag, false);

                await Task.Yield();
                return;
            }

            var qMessage = str.Split("|");
            if (qMessage.Length < 3)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "ConfigTools.LoadRabbitMQManager",
                    Level = Models.SystemLog.Levels.Warn,
                    Message = "Malformed message received:\n" +
                              $"{str}",
                    UserId = -1
                });
                _channel.BasicAck(ea.DeliveryTag, false);

                await Task.Yield();
                return;
            }

            await Task.Yield();
        }
    }
}
