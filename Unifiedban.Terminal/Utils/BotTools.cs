using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unifiedban.Terminal.Bot;

namespace Unifiedban.Terminal.Utils
{
    public class BotTools
    {
        public static bool IsUserOperator(long userId)
        {
            return CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == userId) != null ? true : false;
        }

        public static bool IsUserOperator(long userId, Models.Operator.Levels level)
        {
            return CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == userId &&
                x.Level == level) != null ? true : false;
        }
    }
}
