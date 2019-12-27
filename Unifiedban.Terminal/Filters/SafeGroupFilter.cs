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
        static List<SafeGroup> safeGroups = new List<SafeGroup>();
        public FilterResult DoCheck(Message message)
        {
            return DoCheck(message.Text);
        }
        public static FilterResult DoCheck(string text)
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

        public static void LoadCache()
        {
            BusinessLogic.Group.SafeGroupLogic safeGroupLogic =
                new BusinessLogic.Group.SafeGroupLogic();
            safeGroups = new List<SafeGroup>(safeGroupLogic.Get());
        }
    }
}
