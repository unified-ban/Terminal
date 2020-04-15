/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System;
using System.Text.RegularExpressions;

namespace Unifiedban.Terminal.Utils
{
    public class BotTools
    {
        public static bool IsUserOperator(long userId)
        {
            return CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == userId) != null ? true : false;
        }

        public static bool IsUserOperator(long userId, Models.Operator.Levels level)
        {
            return CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == userId &&
                x.Level >= level) != null ? true : false;
        }

        public static string CurrentVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion;
        }

        public static bool IsValidUrl(string url)
        {
            string regex = @"(((http|ftp|https):\/\/)|(tg:\/\/))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])?|(?![\w_])(@[\w_]+)(?!.)";
            Regex reg = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            MatchCollection matchedWords = reg.Matches(url);

            return matchedWords.Count == 1;
        }
    }

    public class WebClientWithTimeout : System.Net.WebClient
    {
        protected override System.Net.WebRequest GetWebRequest(Uri address)
        {
            System.Net.WebRequest wr = base.GetWebRequest(address);
            wr.Timeout = 5000; // timeout in milliseconds (ms)
            return wr;
        }
    }
}
