using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Help : ICommand
    {
        public Task Execute(Message message)
        {
            InlineKeyboardMarkup menu = JsonConvert.DeserializeObject<InlineKeyboardMarkup>(CacheData.Configuration["HelpMenu"]);

            return Manager.BotClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Help:",
                replyMarkup: menu
            );
        }
    }
}
