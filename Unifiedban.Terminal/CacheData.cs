/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using Telegram.Bot.Types;
using Unifiedban.Plugin.Common;

namespace Unifiedban.Terminal
{
    public class CacheData
    {
        // [[ Program control ]]
        public static string LoggerName = "Unifiedban-Terminal";
        public static bool FatalError = false;
        public static bool IsDisposing = false;
        public static IConfigurationRoot Configuration;
        public static DateTime StartupDateTimeUtc = DateTime.UtcNow;
        public static bool AnswerInvalidCommand = false;
        public static long ControlChatId = 0;
        public static int CaptchaAutoKickTimer = 1;

        // [[ Instance data ]]
        public static List<Models.SysConfig> SysConfigs = new();
        public static List<Models.Operator> Operators = new();
        public static Dictionary<string, Models.Translation.Language> Languages = new();
        public static Dictionary<string, Dictionary<string, Models.Translation.Entry>> Translations = new();
        public static List<Models.Group.ConfigurationParameter> GroupDefaultConfigs = new();
        public static int HandledMessages { get; private set; }
        private static object lockHandledMessages = new();
        
        public static List<UBPlugin> PreCaptchaAndWelcomePlugins = new();
        public static List<UBPlugin> PostCaptchaAndWelcomePlugins = new();
        public static List<UBPlugin> PreFiltersPlugins = new();
        public static List<UBPlugin> PostFiltersPlugins = new();
        public static List<UBPlugin> PreControlsPlugins = new();
        public static List<UBPlugin> PostControlsPlugins = new();

        // [[ Cache ]]
        public static object GroupsLockObj = new ();
        public static readonly Dictionary<long, Models.Group.TelegramGroup> Groups = new();
        public static Dictionary<long, List<Models.Group.ConfigurationParameter>> GroupConfigs = new();
        public static Dictionary<string, Models.Group.NightSchedule> NightSchedules = new();

        public static Dictionary<long, Dictionary<long, UserPrivileges>> ChatAdmins = new();

        public static List<Models.User.Banned> BannedUsers = new();
        public static List<Models.Filters.BadWord> BadWords = new();
        public static List<Utils.ImageHash> BannedImagesHash = new();
        
        public static Dictionary<long, Models.User.TrustFactor> TrustFactors = new();
        public static Dictionary<string, long> Usernames = new();

        public static List<long> ActiveSupport = new();
        public static Dictionary<long, List<long>> CurrentChatOperators = new();

        public static ConcurrentDictionary<string, Timer> CaptchaAutoKickTimers = new();
        public static Dictionary<long, int> CaptchaStrikes = new();
        
        public static List<long> BetaAuthChats = new();

        public static string GetTranslation(
            string languageId,
            string keyId,
            bool firstCapital = false)
        {
            if (!Translations.ContainsKey(languageId))
                return keyId;

            if (!Translations[languageId].ContainsKey(keyId))
            {
                if (!Translations["en"].ContainsKey(keyId))
                {
                    return keyId;
                }

                languageId = "en";
            }

            var value = Translations[languageId][keyId].Translation;

            if (firstCapital)
            {
                return value.Substring(0, 1).ToUpper() + value.Substring(1, value.Length - 1);
            }

            return value;
        }

        public static void IncrementHandledMessages()
        {
            lock (lockHandledMessages)
            {
                HandledMessages++;
            }
        }
    }
}
