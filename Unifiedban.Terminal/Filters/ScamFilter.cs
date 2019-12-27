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

            string regex = @"(http|ftp|https:\/\/)?([\w_-]+\s?(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])?";
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
            
            Models.SysConfig sitesList = CacheData.SysConfigs.Where(x => x.SysConfigId == "PhishingLinks")
                    .SingleOrDefault();
            if (sitesList == null)
                return new FilterResult()
                {
                    CheckName = "ScamFilter",
                    Result = IFilter.FilterResultType.skipped
                };

            using (System.Net.WebClient client = new System.Net.WebClient())
            {

                string htmlCode = "";
                try
                {
                    htmlCode = client.DownloadString(sitesList.Value);
                }
                catch { }
                foreach(Match match in matchedWords)
                    if (htmlCode.Contains(match.Value))
                        return new FilterResult()
                        {
                            CheckName = "ScamFilter",
                            Result = IFilter.FilterResultType.positive
                        };
            }

            return new FilterResult()
            {
                CheckName = "ScamFilter",
                Result = IFilter.FilterResultType.negative
            };
        }
    }
}
