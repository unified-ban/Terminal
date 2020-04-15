/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Unifiedban.Models.Group;

namespace Unifiedban.Terminal.Filters
{
    public class SafeGroupFilter : IFilter
    {
        public SafeGroupFilter()
        {
            LoadCache();
        }

        static List<SafeGroup> safeGroups = new List<SafeGroup>();
        public FilterResult DoCheck(Message message)
        {
            return DoCheck(CacheData.Groups[message.Chat.Id].GroupId,
                message.Text);
        }

        public FilterResult DoCheck(string groupId, string text)
        {
            SafeGroup isKnown = safeGroups
                .Where(x => x.GroupId == groupId && x.GroupName == text)
                .FirstOrDefault();
            if (isKnown == null)
                return new FilterResult()
                {
                    CheckName = "SafeGroup",
                    Result = IFilter.FilterResultType.positive
                };

            return new FilterResult()
            {
                CheckName = "SafeGroup",
                Result = IFilter.FilterResultType.negative
            };
        }

        public static void LoadCache()
        {
            BusinessLogic.Group.SafeGroupLogic safeGroupLogic =
                new BusinessLogic.Group.SafeGroupLogic();
            safeGroups = new List<SafeGroup>(safeGroupLogic.Get());
        }
    }
}
