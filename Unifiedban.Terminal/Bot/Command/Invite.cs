/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using Telegram.Bot.Types;
using Unifiedban.Models;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Invite : ICommand
    {
        public void Execute(Message message)
        {
            try
            {
                var link = CacheData.Groups[message.Chat.Id].InviteLink ??
                           Manager.BotClient.ExportChatInviteLinkAsync(message.Chat).Result;

                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        ReplyToMessageId = message.MessageId,
                        Chat = message.Chat,
                        Text = link,
                        PostSentAction = ChatMessage.PostSentActions.Destroy,
                        AutoDestroyTimeInSeconds = 15
                    });
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