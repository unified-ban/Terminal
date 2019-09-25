using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Help : ICommand
    {
        public Task Execute(Message message)
        {
            InlineKeyboardMarkup menu = 
                JsonConvert.DeserializeObject<InlineKeyboardMarkup>(
                    CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "HelpMenu")
                            .Value);

            return Manager.BotClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Help:",
                replyMarkup: menu
            );
        }
    }
}
