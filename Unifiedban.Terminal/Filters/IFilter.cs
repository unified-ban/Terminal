/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Filters
{
    public interface IFilter
    {
        public enum FilterResultType
        {
            positive,
            negative,
            skipped
        }
        FilterResult DoCheck(Message message);
    }

    public class FilterResult
    {
        public IFilter.FilterResultType Result { get; set; }
        public string CheckName { get; set; }
        public string Rule { get; set; }
    }
}
