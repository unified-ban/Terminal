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
    public class RTLNameFilter : IFilter
    {
        public FilterResult DoCheck(Message message)
        {
            return DoCheck(message, message.From.FirstName + " " + message.From.LastName);
        }
        public FilterResult DoCheck(Message message, string fullName)
        {
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "RTLNameFilter")
                .SingleOrDefault();
            if (configValue != null)
                if (configValue.Value == "false")
                    return new FilterResult()
                    {
                        CheckName = "RTLNameFilter",
                        Result = IFilter.FilterResultType.skipped
                    };

            if(Utils.UserTools.NameIsRTL(fullName))
                return new FilterResult()
                {
                    CheckName = "RTLNameFilter",
                    Result = IFilter.FilterResultType.positive,
                    Rule = "Name has RTL characters"
                };

            return new FilterResult()
            {
                CheckName = "BadWord",
                Result = IFilter.FilterResultType.negative
            };
        }
    }
}
