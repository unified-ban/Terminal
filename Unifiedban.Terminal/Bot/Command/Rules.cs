using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Rules : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            Manager.BotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        parseMode: ParseMode.Markdown,
                        text: CacheData.Groups[message.Chat.Id].RulesText
                        );
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
