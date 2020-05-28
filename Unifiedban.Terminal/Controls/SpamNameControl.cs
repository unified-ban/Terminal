/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Controls
{
    public class SpamNameControl : IControl
    {
        static ConcurrentDictionary<string, bool> isNameSafe = new ConcurrentDictionary<string, bool>();

        public ControlResult DoCheck(Message message)
        {
            if (Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return new ControlResult()
                {
                    CheckName = "Spam Names",
                    Result = IControl.ControlResultType.skipped
                };
            }
            
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "SpamNameControl")
                .SingleOrDefault();
            if (configValue != null)
                if (configValue.Value == "false")
                    return new ControlResult()
                    {
                        CheckName = "Spam Names",
                        Result = IControl.ControlResultType.skipped
                    };

            Filters.BadWordFilter badWordFilter = new Filters.BadWordFilter();
            Filters.FilterResult badName = badWordFilter
                .DoCheck(message, message.From.FirstName + " " + message.From.LastName);
            if(badName.Result == Filters.IFilter.FilterResultType.positive)
                return new ControlResult()
                {
                    CheckName = "Spam Names",
                    Result = IControl.ControlResultType.positive
                };

            if(!String.IsNullOrEmpty(message.From.FirstName))
                if (!isNameSafe.TryGetValue(message.From.FirstName, out bool nameIsValid))
                {
                    CheckIfNameIsValid(message.From.FirstName);
                }

            if (!String.IsNullOrEmpty(message.From.LastName))
                if (!isNameSafe.TryGetValue(message.From.LastName, out bool surnameIsValid))
                {
                    CheckIfNameIsValid(message.From.LastName);
                }

            if (!String.IsNullOrEmpty(message.From.FirstName))
                if (!isNameSafe[message.From.FirstName])
                    return new ControlResult()
                    {
                        CheckName = "Spam Names",
                        Result = IControl.ControlResultType.positive
                    };
            if (!String.IsNullOrEmpty(message.From.LastName))
                if (!isNameSafe[message.From.LastName])
                    return new ControlResult()
                    {
                        CheckName = "Spam Names",
                        Result = IControl.ControlResultType.positive
                    };

            return new ControlResult()
            {
                CheckName = "Spam Names",
                Result = IControl.ControlResultType.negative
            };
        }

        void CheckIfNameIsValid(string name)
        {
            string regex = @"((http|ftp|https):\/\/)?([\w_-]+\s?(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])?";
            Regex reg = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            MatchCollection matchedWords = reg.Matches(name);
            if (matchedWords.Count == 0)
            {
                isNameSafe[name] = true;
                return;
            }
            using (WebClientWithTimeout client = new WebClientWithTimeout())
            {
                string siteUri = name;
                if (!name.Contains("http://") && !name.Contains("https://"))
                    siteUri = "http://" + name;

                string htmlCode = "";
                try
                {
                    htmlCode = client.DownloadString(siteUri);
                }
                catch { }

                if (htmlCode.Contains("tgme_page_extra"))
                    isNameSafe[name] = false;
                else
                    isNameSafe[name] = true;
            }
        }
    }
}
