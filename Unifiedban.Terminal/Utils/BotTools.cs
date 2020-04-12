/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Linq;
using System.Reflection;
using System.Diagnostics;

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
    }
}
