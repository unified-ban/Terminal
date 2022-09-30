/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Mute : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            var sender = message.SenderChat?.Id ?? message.From?.Id ?? 0;
            var isOperator = BotTools.IsUserOperator(sender, Models.Operator.Levels.Basic);
            var isAdmin = ChatTools.IsUserAdmin(message.Chat.Id, sender);
            if (!isOperator && !isAdmin)
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                return;
            }
            else if (!isOperator && isAdmin)
            {
                var adminPermissions = CacheData.ChatAdmins[message.Chat.Id][sender];
                if (!adminPermissions.CanRestrictMembers)
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            Text = CacheData.GetTranslation("en", "error_not_auth_command")
                        });
                    return;
                }
            }

            long userId;

            if (message.ReplyToMessage == null)
            {
                if (!message.Text!.Contains(" "))
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            Text = CacheData.GetTranslation("en", "command_mute_error")
                        });
                    return;
                }
                
                if (message.Text.Split(" ")[1].StartsWith("@"))
                {
                    if (!CacheData.Usernames.Keys.Contains(message.Text.Split(" ")[1].Remove(0, 1)))
                    {
                        MessageQueueManager.EnqueueMessage(
                            new Models.ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                Text = CacheData.GetTranslation("en", "mute_command_error_invalidUsername")
                            });
                        return;
                    }
                    userId = CacheData.Usernames[message.Text.Split(" ")[1].Remove(0, 1)];
                }
                else
                {
                    bool isValid = long.TryParse(message.Text.Split(" ")[1], out userId);
                    if (!isValid)
                    {
                        MessageQueueManager.EnqueueMessage(
                            new Models.ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                Text = CacheData.GetTranslation("en", "mute_command_error_invalidUserId")
                            });
                        return;
                    }
                }
            }
            else
                userId = message.ReplyToMessage.From.Id;
            
            if (BotTools.IsUserOperator(userId))
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "command_to_operator_not_allowed")
                    });

                return;
            }

            try
            {
                Manager.BotClient.RestrictChatMemberAsync(
                    message.Chat.Id,
                    userId,
                    new ChatPermissions()
                    {
                        CanSendMessages = false,
                        CanAddWebPagePreviews = false,
                        CanChangeInfo = false,
                        CanInviteUsers = false,
                        CanPinMessages = false,
                        CanSendMediaMessages = false,
                        CanSendOtherMessages = false,
                        CanSendPolls = false
                    });

                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "mute_command_success")
                    });
            }
            catch
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = CacheData.GetTranslation("en", "command_mute_error")
                    });
            }
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
