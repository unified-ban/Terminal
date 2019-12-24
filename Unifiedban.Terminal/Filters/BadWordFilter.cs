using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Filters
{
    public class BadWordFilter : IFilter
    {
        public FilterResult DoCheck(Message message)
        {
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.Value == "")
                .SingleOrDefault();
            if (configValue != null)
                if (configValue.Value == "false")
                    return new FilterResult()
                    {
                        CheckName = "BadWord",
                        Result = IFilter.FilterResultType.skipped
                    };
            
            List<Models.Filters.BadWord> badWords =
                CacheData.BadWords
                .Where(x => x.GroupId == null || x.GroupId == CacheData.Groups[message.Chat.Id].GroupId)
                .ToList();

            foreach(Models.Filters.BadWord badWord in badWords)
            {
                Regex reg = new Regex(badWord.Regex, RegexOptions.IgnoreCase);
                MatchCollection matchedWords = reg.Matches(message.Text);
                if (matchedWords.Count >= 1)
                    return new FilterResult()
                    {
                        CheckName = "BadWord",
                        Result = IFilter.FilterResultType.positive,
                        Rule = badWord.Name
                    };
            }

            return new FilterResult()
            {
                CheckName = "BadWord",
                Result = IFilter.FilterResultType.negative
            };
        }
    }
}
