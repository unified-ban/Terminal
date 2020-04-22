/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Unifiedban.Terminal.Bot;

namespace Unifiedban.Terminal.Utils
{
    public class ChatTools
    {
        static BusinessLogic.SupportSessionLogLogic logLogic = new BusinessLogic.SupportSessionLogLogic();

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

        public static List<int> GetChatAdminIds(long chatId)
        {
            List<int> admins = new List<int>();
            var administrators = Manager.BotClient.GetChatAdministratorsAsync(chatId).Result;
            foreach (ChatMember member in administrators)
            {
                admins.Add(member.User.Id);
            }
            return admins;
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

            Task.Run(() => RecordSupportSessionMessage(message));
        }

        private static void RecordSupportSessionMessage(Message message)
        {
            Models.SupportSessionLog.SenderType senderType = Models.SupportSessionLog.SenderType.User;
            if (BotTools.IsUserOperator(message.From.Id))
                senderType = Models.SupportSessionLog.SenderType.Operator;
            else if (CacheData.CurrentChatAdmins[message.Chat.Id]
                    .Contains(message.From.Id))
                senderType = Models.SupportSessionLog.SenderType.Admin;

            Models.SupportSessionLog log = new Models.SupportSessionLog()
            {
                GroupId = CacheData.Groups[message.Chat.Id].GroupId,
                SenderId = message.From.Id,
                Text = message.Text,
                Timestamp = DateTime.UtcNow,
                Type = senderType
            };
            logLogic.Add(log, -2);
        }
    }
}
