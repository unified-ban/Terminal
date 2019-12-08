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

            if (message.ReplyToMessage == null && message.ForwardFromMessageId == 0)
            {
                dataMessage += "<b>Message Id:</b> {{message_id}}\n";
                dataMessage += "<b>From chat Id:</b> {{chat_id}}\n";
                dataMessage += "<b>From user Id:</b> {{from_id}}\n";
                dataMessage += "<b>Username:</b> {{from_username}}\n";
                dataMessage += "<b>Is bot:</b> {{from_isBot}}\n\n";
                dataMessage += "<b>Chat hash code:</b> #UB{{chat_id_noMinus}}_" + operationGuid;
            }
            else if (message.ReplyToMessage != null)
            {
                if (message.ReplyToMessage.ForwardFrom != null)
                {
                    dataMessage += "<b>➡️↩️ Message Id:</b> {{replyToMessage_forwardFrom_message_id}}\n";
                    dataMessage += "<b>From chat Id:</b> {{replyToMessage_forwardFrom_chat_id}}\n";
                    dataMessage += "<b>From user Id:</b> {{replyToMessage_forwardFrom_from_id}}\n";
                    dataMessage += "<b>Username:</b> {{replyToMessage_forwardFrom_from_username}}\n";
                    dataMessage += "<b>Is bot:</b> {{replyToMessage_forwardFrom_from_isBot}}\n\n";
                    dataMessage += "<b>Chat hash code:</b> #UB{{chat_id_noMinus}}_" + operationGuid;
                }
                else
                {
                    dataMessage += "<b>↩️ Message Id:</b> {{replyToMessage_message_id}}\n";
                    dataMessage += "<b>From chat Id:</b> {{replyToMessage_chat_id}}\n";
                    dataMessage += "<b>From user Id:</b> {{replyToMessage_from_id}}\n";
                    dataMessage += "<b>Username:</b> {{replyToMessage_from_username}}\n";
                    dataMessage += "<b>Is bot:</b> {{replyToMessage_from_isBot}}\n\n";
                    dataMessage += "<b>Chat hash code:</b> #UB{{chat_id_noMinus}}_" + operationGuid;
                }
            }
            else if (message.ForwardFrom != null)
            {
                dataMessage += "<b>➡️ Message Id:</b> {{forwardFrom_message_id}}\n";
                dataMessage += "<b>From chat Id:</b> {{forwardFrom_chat_id}}\n";
                dataMessage += "<b>From user Id:</b> {{forwardFrom_from_id}}\n";
                dataMessage += "<b>Username:</b> {{forwardFrom_from_username}}\n";
                dataMessage += "<b>Is bot:</b> {{forwardFrom_from_isBot}}\n\n";
                dataMessage += "<b>Chat hash code:</b> #UB{{chat_id_noMinus}}_" + operationGuid;
            }

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
