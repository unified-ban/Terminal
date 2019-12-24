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
