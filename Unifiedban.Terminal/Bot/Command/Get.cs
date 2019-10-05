using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Get : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            string operationGuid = Guid.NewGuid().ToString().Replace('-', '_');
            string dataMessage = "<b>[Report]</b>\nRequested information:\n";
            dataMessage += "<b>Message Id:</b> {{message_id}}\n";
            dataMessage += "<b>From chat Id:</b> {{chat_id}}\n";
            dataMessage += "<b>From user Id:</b> {{from_id}}\n";
            dataMessage += "<b>Username:</b> {{from_username}}\n";
            dataMessage += "<b>Is bot:</b> {{from_isBot}}\n\n";
            dataMessage += "<b>Chat hash code:</b> #UB{{chat_id_noMinus}}_" + operationGuid;

            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Html,
                    Text = Utils.Parsers.VariablesParser(dataMessage, message)
                });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
