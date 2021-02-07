/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using Telegram.Bot.Types;
using Unifiedban.Models;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Alias : ICommand
    {
        public void Execute(Message message)
        {
            try
            {
                var link = CacheData.Groups[message.Chat.Id].InviteAlias != null ?
                            CacheData.Configuration["GroupAliasLinkPrefix"] + CacheData.Groups[message.Chat.Id].InviteAlias :
                           "No alias set for this group.";

                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        ReplyToMessageId = message.MessageId,
                        Chat = message.Chat,
                        Text = link
                    });
            }
            catch
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = "Error getting alias link.",
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