/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Unifiedban.Models;

namespace Unifiedban.Terminal.Bot.Command
{
    public class InviteToPrivate : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            
            var me = Manager.BotClient.GetChatMemberAsync(message.Chat.Id, Manager.MyId).Result;
            if (me is ChatMemberAdministrator { CanInviteUsers: false }) return;

            var memberAdmin = Manager.BotClient.GetChatAdministratorsAsync(message.Chat.Id).Result
                .Single(x => x.User.Id == message.From.Id);
            if (memberAdmin is ChatMemberAdministrator { CanInviteUsers: false }) return;

            try
            {
                var link = CacheData.Groups[message.Chat.Id].InviteLink ??
                           Manager.BotClient.ExportChatInviteLinkAsync(message.Chat).Result;

                var msg = $"Invite link for chat {message.Chat.Title} is {link}";
                Manager.BotClient.SendTextMessageAsync(message.From.Id, msg);
            }
            catch
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = "Error generating invite link.",
                        PostSentAction = ChatMessage.PostSentActions.Destroy,
                        AutoDestroyTimeInSeconds = 10
                    });
            }
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            return;
        }
    }
}