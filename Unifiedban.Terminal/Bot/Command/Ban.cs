﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Ban : ICommand
    {
        public void Execute(Message message)
        {
            if (!BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Basic) &&
                !ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
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

            if (Manager.BotClient.GetChatAdministratorsAsync(message.Chat.Id).Result
                .Single(x => x.User.Id == message.From.Id)
                .CanRestrictMembers == false)
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

            int userToKick;

            if (message.ReplyToMessage == null)
            {
                if (message.Text.Split(" ")[1].StartsWith("@"))
                {
                    if(!CacheData.Usernames.Keys.Contains(message.Text.Split(" ")[1].Remove(0, 1)))
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
                    userToKick = CacheData.Usernames[message.Text.Split(" ")[1].Remove(0, 1)];
                }
                else
                {
                    bool isValid = int.TryParse(message.Text.Split(" ")[1], out userToKick);
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
                userToKick = message.ReplyToMessage.From.Id;
            
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

            if (BotTools.IsUserOperator(userToKick))
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
                Manager.BotClient.KickChatMemberAsync(message.Chat.Id, userToKick,
                    DateTime.UtcNow.AddMinutes(-5));
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
            
            UserTools.AddPenality(userToKick,
                Models.TrustFactorLog.TrustFactorAction.ban, Manager.MyId);
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
