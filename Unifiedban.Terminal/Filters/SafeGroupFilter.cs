/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            return DoCheck(message.Text);
        }
        public FilterResult DoCheck(string text)
        {
            SafeGroup isKnown = safeGroups
                .Where(x => x.GroupName == text)
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

        private static void LoadCache()
        {
            BusinessLogic.Group.SafeGroupLogic safeGroupLogic =
                new BusinessLogic.Group.SafeGroupLogic();
            safeGroups = new List<SafeGroup>(safeGroupLogic.Get());
        }
    }
}
