/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;
using Unifiedban.Terminal.Bot;

namespace Unifiedban.Terminal.Utils
{
    public class ChatTools
    {
        public static bool IsUserAdmin(long chatId, long userId)
        {
            var administrators = Bot.Manager.BotClient.GetChatAdministratorsAsync(chatId).Result;
            foreach(Telegram.Bot.Types.ChatMember member in administrators)
            {
                if (member.User.Id == userId)
                    return true;
            }
            return false;
        }

        public static void HandleSupportSessionMsg(Message message)
        {
            if (message.Text != null)
            if (message.Text.StartsWith("/"))
                return;

            if (!CacheData.ActiveSupport
                .Contains(message.Chat.Id))
                return;

            if (BotTools.IsUserOperator(message.From.Id))
            {
                Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                ChatMessage newMsg = new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    Text = message.Text +
                        "\n\nMessage from operator: " + message.From.Username
                };
                if (message.ReplyToMessage != null)
                    newMsg.ReplyToMessageId = message.ReplyToMessage.MessageId;
                MessageQueueManager.EnqueueMessage(newMsg);
            }

            RecordSupportSessionMessage(message);
        }

        private static void RecordSupportSessionMessage(Message message)
        {

        }
    }
}
