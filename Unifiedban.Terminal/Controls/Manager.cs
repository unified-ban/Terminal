using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Controls
{
    public class Manager
    {
        static List<Filters.IFilter> filters = new List<Filters.IFilter>();
        public static void Initialize()
        {
            filters.Add(new Filters.BadWordFilter());
        }

        public static void DoCheck(Message message)
        {
            foreach(Filters.IFilter filter in filters)
            {
                Filters.FilterResult result = filter.DoCheck(message);
                if(result.Result == Filters.IFilter.FilterResultType.positive)
                {
                    RemoveMessageForPositiveFilter(message, result);
                    return;
                }
            }
        }

        private static void RemoveMessageForPositiveFilter(Message message, Filters.FilterResult result)
        {
            Bot.Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "Message deleted due to control *{0}* provided positive result on rule *{1}*.",
                        result.CheckName,
                        result.Rule)
                );
        }
    }
}
