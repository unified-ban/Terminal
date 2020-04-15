/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Filters
{
    public class ScamFilter : IFilter
    {
        static string linksList = "";
        static DateTime lastUpdate = DateTime.UtcNow.AddDays(-2);
        static List<string> safeSingleLetter;

        public FilterResult DoCheck(Message message)
        {
            return DoCheck(message, message.Text);
        }

        public FilterResult DoCheck(Message message, string text)
        {
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "ScamFilter")
                .SingleOrDefault();
            if (configValue != null)
                if (configValue.Value == "false")
                    return new FilterResult()
                    {
                        CheckName = "ScamFilter",
                        Result = IFilter.FilterResultType.skipped
                    };

            string regex = @"(http|ftp|https:\/\/)?([\w_-]+\s?(?:(?:\.[\w_-]{2,})+))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])?";
            Regex reg = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            MatchCollection matchedWords = reg.Matches(text);
            if (matchedWords.Count == 0)
            {
                return new FilterResult()
                {
                    CheckName = "ScamFilter",
                    Result = IFilter.FilterResultType.negative
                };
            }

            if (lastUpdate < DateTime.UtcNow.AddDays(-1))
                updateLinksList();

            if (String.IsNullOrEmpty(linksList))
                return new FilterResult()
                {
                    CheckName = "ScamFilter",
                    Result = IFilter.FilterResultType.skipped
                };
            
            foreach (Match match in matchedWords)
                if (linksList.Contains(match.Value) && 
                    !safeSingleLetter.Contains(match.Value))
                    return new FilterResult()
                    {
                        CheckName = "ScamFilter",
                        Result = IFilter.FilterResultType.positive
                    };

            return new FilterResult()
            {
                CheckName = "ScamFilter",
                Result = IFilter.FilterResultType.negative
            };
        }

        void updateLinksList()
        {
            safeSingleLetter = new List<string>();
            safeSingleLetter.Add("a.co");
            safeSingleLetter.Add("a.org");
            safeSingleLetter.Add("b.org");
            safeSingleLetter.Add("e.im");
            safeSingleLetter.Add("g.co");
            safeSingleLetter.Add("i.net");
            safeSingleLetter.Add("m.me");
            safeSingleLetter.Add("n.pr");
            safeSingleLetter.Add("o.co");
            safeSingleLetter.Add("q.com");
            safeSingleLetter.Add("q.net");
            safeSingleLetter.Add("s.co");
            safeSingleLetter.Add("s.de");
            safeSingleLetter.Add("t.com");
            //safeSingleLetter.Add("t.me");
            safeSingleLetter.Add("u.ae");
            safeSingleLetter.Add("w.org");
            safeSingleLetter.Add("y.org");
            safeSingleLetter.Add("x.com");
            safeSingleLetter.Add("x.org");
            safeSingleLetter.Add("z.com");

            Models.SysConfig sitesList = CacheData.SysConfigs.Where(x => x.SysConfigId == "PhishingLinks")
                    .SingleOrDefault();
            if (sitesList == null)
                return;

            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                try
                {
                    linksList = client.DownloadString(sitesList.Value);
                }
                catch (Exception ex)
                {
                    Data.Utils.Logging.AddLog(new Models.SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Terminal.Filters.ScamFilter.updateLinksList",
                        Level = Models.SystemLog.Levels.Error,
                        Message = "Error getting updated phishing links!",
                        UserId = -1
                    });
                    Data.Utils.Logging.AddLog(new Models.SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Terminal.Filters.ScamFilter.updateLinksList",
                        Level = Models.SystemLog.Levels.Error,
                        Message = ex.Message,
                        UserId = -1
                    });
                }
            }
        }
    }
}
