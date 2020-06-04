/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Controls
{
    public class SafeGroupControl : IControl
    {
        Filters.SafeGroupFilter safeGroupFilter = new Filters.SafeGroupFilter();

        public ControlResult DoCheck(Message message)
        {
            if (Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return new ControlResult()
                {
                    CheckName = "Safe Group",
                    Result = IControl.ControlResultType.skipped
                };
            }

            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "SafeGroupControl")
                .SingleOrDefault();
            if (configValue != null)
                if (configValue.Value == "false")
                    return new ControlResult()
                    {
                        CheckName = "Safe Group",
                        Result = IControl.ControlResultType.skipped
                    };

            string regex = @"(((http|https):\/\/)|(tg:\/\/)|(t.me\/))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])?|(?![\w_])(@[\w_]+)(?!.)";
            Regex reg = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            MatchCollection matchedWords = reg.Matches(message.Text);
            if (matchedWords.Count == 0)
            {
                return new ControlResult()
                {
                    CheckName = "Safe Group",
                    Result = IControl.ControlResultType.negative
                };

            }
            
            foreach (Match match in matchedWords)
            {
                string url = match.Value;
                if (url.StartsWith("@"))
                    url = "https://t.me/" + match.Value.Remove(0, 1);
                if (url.StartsWith("t.me"))
                    url = "https://" + match.Value;
                if (url.StartsWith("http"))
                    url = url.Replace("http", "https");

                if (url.Contains("/c/"))
                {
                    if (url.Split("/c/")[1].Split('/')[0] == message.Chat.Id.ToString())
                    {
                        return new ControlResult()
                        {
                            CheckName = "Safe Group",
                            Result = IControl.ControlResultType.skipped
                        };
                    }
                }

                if (url == "https://t.me/unifiedban_group" ||
                    url == "https://t.me/unifiedban_news" ||
                    url == "https://t.me/unifiedban_bot" ||
                    url == "https://t.me/unifiedbanBeta_bot" ||
                    url == "https://t.me/joinchat/B35YY0QbLfd034CFnvCtCA" || // Support chat of the TelegramBots library
                    url == "https://t.me/dotnetgram") // .NET global discussion and support chat
                {
                    return new ControlResult()
                    {
                        CheckName = "Safe Group",
                        Result = IControl.ControlResultType.skipped
                    };
                }

                if (Manager.IsTelegramLink(url))
                    if (safeGroupFilter.DoCheck(
                        CacheData.Groups[message.Chat.Id].GroupId, url)
                            .Result == Filters.IFilter.FilterResultType.positive)
                        return new ControlResult()
                        {
                            CheckName = "Safe Group",
                            Result = IControl.ControlResultType.positive
                        };
            }

            return new ControlResult()
            {
                CheckName = "Safe Group",
                Result = IControl.ControlResultType.negative
            };
        }
    }
}
