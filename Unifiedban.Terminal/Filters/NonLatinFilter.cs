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
            if (Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return new FilterResult()
                {
                    CheckName = "BadWord",
                    Result = IFilter.FilterResultType.skipped
                };
            }
            
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

            string regex = @"[^\x00-\x7FÀ-ÖØ-öø-ÿ"; // non latin chars
            regex += @"\p{IsCurrencySymbols}\p{IsMiscellaneousSymbols}\p{IsMiscellaneousTechnical}";
            regex += @"p{IsArrows}\p{IsMiscellaneousSymbolsandArrows}\p{IsMathematicalOperators}]+";

            Regex reg = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            MatchCollection matchedWords = reg.Matches(removeEmojis(message.Text));
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

        private string removeEmojis(string text)
        {
            string regex = @"(¯\\_\(ツ\)_\/¯)|(_\/\(ツ\)\\_)|\( ͡° ͜ʖ ͡°\)|" + // commonly used smiles
                           @"(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])"; // Emojis

            Regex reg = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            MatchCollection matchedWords = reg.Matches(text);
            foreach (Match match in matchedWords)
                text = text.Replace(match.Value, string.Empty);

            text = Regex.Replace(text, @"\uFE0F+", string.Empty); // remove all Control and non-printable chars
            text = Regex.Replace(text, @"º|µ|¶|«|»|´|¿|¡|µ|¾|½|¼|¤|¹|²|³|¤|×|¨|°|÷|£|¢|’|ª|·", string.Empty); // whitelisted chars
            return text;
        }
    }
}
