/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Hangfire;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Utils
{
    public class ConfigTools
    {
        static BusinessLogic.Group.TelegramGroupLogic telegramGroupLogic =
            new BusinessLogic.Group.TelegramGroupLogic();
        static BusinessLogic.Group.NightScheduleLogic nsl =
            new BusinessLogic.Group.NightScheduleLogic();

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
        }

        public static void Dispose()
        {
            SyncGroupsToDatabase();
            SyncGroupsConfigToDatabase();
            SyncWelcomeAndRulesText();
            SyncNightScheduleToDatabase();
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
    }
}
