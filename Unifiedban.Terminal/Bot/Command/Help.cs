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
        public void Execute(Message message)
        {
            InlineKeyboardMarkup menu = 
                JsonConvert.DeserializeObject<InlineKeyboardMarkup>(
                    CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "HelpMenu")
                            .Value);

            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    Text = "*[Help:]*",
                    ReplyMarkup = menu
                });
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            return;
        }
    }
}
