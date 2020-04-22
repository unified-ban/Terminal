/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal
{
    public class CacheData
    {
        // [[ Program control ]]
        public static string LoggerName = "Unifiedban-Terminal";
        public static bool FatalError = false;
        public static IConfigurationRoot Configuration;
        public static DateTime StartupDateTimeUtc = DateTime.UtcNow;
        public static bool AnswerInvalidCommand = false; 

        // Instance data
        public static List<Models.SysConfig> SysConfigs = new List<Models.SysConfig>();
        public static List<Models.Operator> Operators = new List<Models.Operator>();
        public static Dictionary<string, Models.Translation.Language> Languages = 
            new Dictionary<string, Models.Translation.Language>();
        public static Dictionary<string, Dictionary<string, Models.Translation.Entry>> Translations = 
            new Dictionary<string, Dictionary<string, Models.Translation.Entry>>();

        public static List<Models.Group.ConfigurationParameter> GroupDefaultConfigs =
            new List<Models.Group.ConfigurationParameter>();
        public static Dictionary<long, Models.Group.TelegramGroup> Groups =
            new Dictionary<long, Models.Group.TelegramGroup>();
        public static Dictionary<long, List<Models.Group.ConfigurationParameter>> GroupConfigs =
            new Dictionary<long, List<Models.Group.ConfigurationParameter>>();

        public static List<Models.User.Banned> BannedUsers = new List<Models.User.Banned>();
        public static Dictionary<string, int> Usernames = new Dictionary<string, int>();

        public static List<long> ActiveSupport =
            new List<long>();

        public static long ControlChatId = 0;

        public static int HandledMessages { get; private set; }
        private static object lockHandledMessages = new object();

        public static string GetTranslation(
            string languageId,
            string keyId,
            bool firstCapital = false)
        {
            if (!Translations.ContainsKey(languageId))
                return keyId;

            if (!Translations[languageId].ContainsKey(keyId))
                if (!Translations["en"].ContainsKey(keyId))
                    return keyId;

            string value = Translations[languageId][keyId].Translation;

            if (firstCapital)
                return value.Substring(0, 1).ToUpper() + value.Substring(1, value.Length - 1);
            
            return value;
        }

        public static void IncrementHandledMessages()
        {
            lock (lockHandledMessages)
            {
                HandledMessages++;
            }
        }

        // [[ Cache ]]
        public static List<Utils.ImageHash> BannedImagesHash =
            new List<Utils.ImageHash>();
        public static List<Models.Filters.BadWord> BadWords = new List<Models.Filters.BadWord>();

    }
}
