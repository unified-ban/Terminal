/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Hangfire;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Unifiedban.Terminal.Utils
{
    public class ConfigTools
    {
        public static void Initialize()
        {
            RecurringJob.AddOrUpdate("ConfigTools_SyncGroupsConfigToDatabase", () => SyncGroupsConfigToDatabase(), "0/30 * * ? * *");
            RecurringJob.AddOrUpdate("ConfigTools_SyncWelcomeAndRulesText", () => SyncWelcomeAndRulesText(), "0/30 * * ? * *");
        }

        public static void SyncGroupsConfigToDatabase()
        {
            BusinessLogic.Group.TelegramGroupLogic telegramGroupLogic =
                new BusinessLogic.Group.TelegramGroupLogic();
            
            foreach (long group in CacheData.GroupConfigs.Keys)
                telegramGroupLogic.UpdateConfiguration(
                    group,
                    JsonConvert.SerializeObject(CacheData.GroupConfigs[group]),
                    -2);
        }

        public static void SyncWelcomeAndRulesText()
        {
            BusinessLogic.Group.TelegramGroupLogic telegramGroupLogic =
                new BusinessLogic.Group.TelegramGroupLogic();

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
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool UpdateRulesText(long groupId, string text)
        {
            try
            {
                CacheData.Groups[groupId].RulesText = text;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
