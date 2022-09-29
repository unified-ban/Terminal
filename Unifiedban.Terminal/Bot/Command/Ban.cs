/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Unifiedban.Data.Utils;
using Unifiedban.Models;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Ban : ICommand
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
                        Text = CacheData.GetTranslation("en", "ban_command_error_notadmin")
                    });
                return;
            }
            
            var me = Manager.BotClient.GetChatMemberAsync(message.Chat.Id, Manager.MyId).Result;
            if (me is ChatMemberAdministrator { CanRestrictMembers: false })
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
            
            if (isAdmin)
            {
                var adminPermissions = CacheData.ChatAdmins[message.Chat.Id][sender];
                if (!adminPermissions.CanRestrictMembers)
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

            long userToKick;

            if (message.ReplyToMessage == null)
            {
                if (!message.Text.Contains(" "))
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            Text = CacheData.GetTranslation("en", "ban_command_error_invalidUserId")
                        });
                    return;
                }
                
                if (message.Text.Split(" ")[1].StartsWith("@"))
                {
                    var cleanUsername = message.Text.Split(" ")[1].Remove(0, 1);
                    if(!CacheData.Usernames.Keys.Contains(cleanUsername))
                    {
                        MessageQueueManager.EnqueueMessage(
                            new Models.ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                Text = CacheData.GetTranslation("en", "ban_command_error_invalidUsername")
                            });
                        return;
                    }
                    userToKick = CacheData.Usernames[cleanUsername];
                }
                else
                {
                    bool isValid = long.TryParse(message.Text.Split(" ")[1], out userToKick);
                    if (!isValid)
                    {
                        MessageQueueManager.EnqueueMessage(
                            new Models.ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                Text = CacheData.GetTranslation("en", "ban_command_error_invalidUserId")
                            });
                        return;
                    }
                }
            }
            else
                userToKick = message.ReplyToMessage!.SenderChat?.Id ?? message.ReplyToMessage.From!.Id;
           
            if (userToKick == 777000) // Telegram's official updateServiceNotification
            {
                Manager.BotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "*[Error]*\n" +
                        "This is an official Telegram's user/id.")
                );

                return;
            }

            /*if (BotTools.IsUserOperator(userToKick))
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "command_to_operator_not_allowed")
                    });

                return;
            }*/
            
            try
            {
                Manager.BotClient.BanChatMemberAsync(message.Chat.Id, userToKick,
                    DateTime.UtcNow.AddSeconds(10));
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "ban_command_success")
                    });
            }
            catch
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "ban_command_error")
                    });
                return;
            }
            
            UserTools.AddPenalty(message.Chat.Id, userToKick,
                Models.TrustFactorLog.TrustFactorAction.ban, Manager.MyId);
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
