using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Controls
{
    public class Manager
    {
        static List<Controls.IControl> controls = new List<Controls.IControl>();
        static List<Filters.IFilter> filters = new List<Filters.IFilter>();

        public static void Initialize()
        {
            controls.Add(new FloodControl());

            filters.Add(new Filters.BadWordFilter());
            Filters.BadWordFilter.BuildDictionary();
            filters.Add(new Filters.NonLatinFilter());
        }

        public static void DoCheck(Message message)
        {
            foreach (Controls.IControl control in controls)
            {
                Controls.ControlResult result = control.DoCheck(message);
                if (result.Result == Controls.IControl.ControlResultType.positive)
                {
                    RemoveMessageForPositiveControl(message, result);
                    return;
                }
            }

            foreach (Filters.IFilter filter in filters)
            {
                Filters.FilterResult result = filter.DoCheck(message);
                if(result.Result == Filters.IFilter.FilterResultType.positive)
                {
                    RemoveMessageForPositiveFilter(message, result);
                    return;
                }
            }
        }

        public static void DoMediaCheck(Message message)
        {
            // TODO !
        }

        private static void RemoveMessageForPositiveControl(Message message, Controls.ControlResult result)
        {
            Bot.Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "Message deleted due to control *{0}* provided positive result.",
                        result.CheckName)
                );
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
                        "Message deleted due to filter *{0}* provided positive result on rule *{1}*.",
                        result.CheckName,
                        result.Rule)
                );
        }
    }
}
