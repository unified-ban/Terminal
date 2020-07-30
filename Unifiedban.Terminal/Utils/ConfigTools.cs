 /* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Hangfire;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Unifiedban.Models.Group;

namespace Unifiedban.Terminal.Utils
{
    public class ConfigTools
    {
        static BusinessLogic.Group.TelegramGroupLogic telegramGroupLogic =
            new BusinessLogic.Group.TelegramGroupLogic();
        static BusinessLogic.Group.NightScheduleLogic nsl =
            new BusinessLogic.Group.NightScheduleLogic();
        
        static Timer keepWSAlive;
        static HubConnection connection;

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
            
            keepWSAlive = new Timer(1000 * 20);
            keepWSAlive.Elapsed += KeepWSAliveOnElapsed;
            keepWSAlive.AutoReset = true;

            connectToHub();
        }

        private static void KeepWSAliveOnElapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static void Dispose()
        {
            SyncGroupsToDatabase();
            SyncGroupsConfigToDatabase();
            SyncWelcomeAndRulesText();
            SyncNightScheduleToDatabase();
            
            keepWSAlive.Stop();
            if (connection.State == HubConnectionState.Connected)
            {
                connection.StopAsync().Wait();
            }
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

        private static void connectToHub()
        {
            if (CacheData.FatalError ||
                CacheData.IsDisposing)
            {
                return;
            }
            
            connection = new HubConnectionBuilder()
                .WithUrl(CacheData.Configuration["HubServerAddress"])
                .WithAutomaticReconnect()
                .Build();

            connection.StartAsync().Wait();
            connection.InvokeAsync("Identification", CacheData.Configuration["HubServerToken"]);

            connection.On<string, string, string, string>("UpdateSetting", 
                (dashboardUserId, groupId, parameter, value) =>
            {
                if (CacheData.FatalError ||
                    CacheData.IsDisposing)
                {
                    return;
                }

                HandleWsCommand(dashboardUserId, groupId, parameter, value);
            });
            
            connection.On<string, string, string, bool>("ToggleStatus", 
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

            if (parameter == "WelcomeText")
            {
                CacheData.Groups[group.TelegramChatId].WelcomeText = value;
                return;
            }
            else if (parameter == "RulesText") 
            {
                CacheData.Groups[group.TelegramChatId].RulesText = value;
                return;
            }
            else if (parameter == "ReportChatId")
            {
                if (!Int64.TryParse(value, out long reportChatId))
                {
                    return;
                }
                CacheData.Groups[group.TelegramChatId].ReportChatId = reportChatId;
                return;
            }

            ConfigurationParameter config = CacheData.GroupConfigs[group.TelegramChatId]
                .Where(x => x.ConfigurationParameterId == parameter)
                .SingleOrDefault();
            if (config == null)
                return;

            CacheData.GroupConfigs[group.TelegramChatId]
                [CacheData.GroupConfigs[group.TelegramChatId]
                    .IndexOf(config)]
                .Value = value;
        }
    }
}
