/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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

            string dataMessage = "*[Report]*\nRequested information:\n\n";

            if (message.ReplyToMessage == null && message.ForwardFromMessageId == 0)
            {
                dataMessage += "*Message Id:* {{message_id}}\n";
                dataMessage += "*From chat Id:* {{chat_id}}\n";
                dataMessage += "*From user Id:* {{from_id}}\n";
                dataMessage += "*Username:* {{from_username}}\n";
                dataMessage += "*Is bot:* {{from_isBot}}\n";
            }
            else if (message.ReplyToMessage != null)
            {
                if (message.ReplyToMessage.ForwardFrom != null)
                {
                    dataMessage += "*➡️↩️ Message Id:* {{replyToMessage_forwardFrom_message_id}}\n";
                    dataMessage += "*From chat Id:* {{replyToMessage_forwardFrom_chat_id}}\n";
                    dataMessage += "*From user Id:* {{replyToMessage_forwardFrom_from_id}}\n";
                    dataMessage += "*Username:* {{replyToMessage_forwardFrom_from_username}}\n";
                    dataMessage += "*Is bot:* {{replyToMessage_forwardFrom_from_isBot}}\n";
                }
                else
                {
                    dataMessage += "*↩️ Message Id:* {{replyToMessage_message_id}}\n";
                    dataMessage += "*From chat Id:* {{replyToMessage_chat_id}}\n";
                    dataMessage += "*From user Id:* {{replyToMessage_from_id}}\n";
                    dataMessage += "*Username:* {{replyToMessage_from_username}}\n";
                    dataMessage += "*Is bot:* {{replyToMessage_from_isBot}}\n";
                }
            }
            else if (message.ForwardFrom != null)
            {
                dataMessage += "*➡️ Message Id:* {{forwardFrom_message_id}}\n";
                dataMessage += "*From chat Id:* {{forwardFrom_chat_id}}\n";
                dataMessage += "*From user Id:* {{forwardFrom_from_id}}\n";
                dataMessage += "*Username:* {{forwardFrom_from_username}}\n";
                dataMessage += "*Is bot:* {{forwardFrom_from_isBot}}\n";
            }
            
            string parsedText = Utils.Parsers.VariablesParser(dataMessage, message);
            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = parsedText
                });
            Manager.BotClient.SendTextMessageAsync(
                chatId: CacheData.ControlChatId,
                parseMode: ParseMode.Markdown,
                text: parsedText + String.Format(
                    "\n*hash_code:* #UB{0}-{1}",
                    message.Chat.Id.ToString().Replace("-",""),
                    Guid.NewGuid())
            );
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
