﻿/* This Source Code Form is subject to the terms of the Mozilla Public
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
    public class Unban : ICommand
    {
        public void Execute(Message message)
        {
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
                        Text = CacheData.GetTranslation("en", "unban_command_error_notadmin")
                    });
                return;
            }

            if (isAdmin)
            {
                if (!ChatTools.IsUserAdmin(message.Chat.Id, Manager.MyId))
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            Text = CacheData.GetTranslation("en", "ban_command_error_adminPrivilege")
                        });
                    return;
                }

                if (!CacheData.ChatAdmins[message.Chat.Id][Manager.MyId].CanRestrictMembers)
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            Text = CacheData.GetTranslation("en", "ban_command_error_adminPrivilege")
                        });
                    return;
                }
            }
            else if (!isOperator)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "ban_command_error_adminPrivilege")
                    });
                return;
            }

            if (message.Chat.Type != ChatType.Supergroup)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "unban_command_notSuperGroup")
                    });
                return;
            }

            long userId;

            if (message.ReplyToMessage == null)
            {
                if (!message.Text.Contains(" "))
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            Text = CacheData.GetTranslation("en", "unban_command_error_invalidUserId")
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
                                Text = CacheData.GetTranslation("en", "unban_command_error_invalidUsername")
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
                                Text = CacheData.GetTranslation("en", "unban_command_error_invalidUserId")
                            });
                        return;
                    }
                }
            }
            else
                userId = message.ReplyToMessage!.SenderChat?.Id ?? message.ReplyToMessage.From!.Id;

            try
            {
                
                Manager.BotClient.UnbanChatMemberAsync(message.Chat.Id, userId);
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "unban_command_success")
                    });
            }
            catch
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "unban_command_error")
                    });
            }
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
