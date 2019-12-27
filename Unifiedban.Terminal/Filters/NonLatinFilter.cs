/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Unifiedban.Models.Filters;

namespace Unifiedban.Terminal.Filters
{
    public class NonLatinFilter : IFilter
    {
        public FilterResult DoCheck(Message message)
        {
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "NonLatinFilter")
                .SingleOrDefault();
            if (configValue != null)
                if (configValue.Value == "false")
                    return new FilterResult()
                    {
                        CheckName = "Non-Latin Filter",
                        Result = IFilter.FilterResultType.skipped
                    };

            string regex = @"[^\x00-\x7FÀ-ÖØ-öø-ÿ\p{Sc}]+";

            Regex reg = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            MatchCollection matchedWords = reg.Matches(message.Text);
            if (matchedWords.Count > 0)
                return new FilterResult()
                {
                    CheckName = "Non-Latin Filter",
                    Result = IFilter.FilterResultType.positive,
                    Rule = "Non-Latin Filter"
                };

            return new FilterResult()
            {
                CheckName = "Non-Latin Filter",
                Result = IFilter.FilterResultType.negative
            };
        }
    }
}
