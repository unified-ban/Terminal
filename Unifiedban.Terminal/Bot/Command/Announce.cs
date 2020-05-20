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
    public class Announce : ICommand
    {
        public void Execute(Message message)
        {
            if (!Utils.BotTools.IsUserOperator(message.From.Id) &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "User *{0}:{1}* tried to use command Announce.",
                        message.From.Id,
                        message.From.Username)
                );
                return;
            }

            string command = message.Text.Split(" ")[0].Remove(0, 1);
            if (command.Contains("@"))
            {
                if (!String.Equals(command.Split("@")[1],
                    Manager.Username, StringComparison.CurrentCultureIgnoreCase))
                    return;
                command = command.Split("@")[0];
            }
            message.Text = message.Text.Remove(0, command.Length + 2);

            string messageHeader = CacheData.GetTranslation("en", "command_announce_header");
            string parsedMessage = messageHeader + "\n" + message.Text;
            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Html,
                    Text = parsedMessage,
                    PostSentAction = ChatMessage.PostSentActions.Pin
                });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
