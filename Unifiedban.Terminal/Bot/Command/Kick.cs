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
    public class Kick : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            if (!BotTools.IsUserOperator(message.From.Id) &&
                !ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
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
                            Text = CacheData.GetTranslation("en", "kick_command_error_invalidUserId")
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
                                Text = CacheData.GetTranslation("en", "kick_command_error_invalidUsername")
                            });
                        return;
                    }
                    userToKick = CacheData.Usernames[message.Text.Split(" ")[1].Remove(0, 1)];
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
                                Text = CacheData.GetTranslation("en", "kick_command_error_invalidUserId")
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
            
            try
            {
                Manager.BotClient.KickChatMemberAsync(message.Chat.Id, userToKick);
                if (message.Chat.Type == ChatType.Supergroup)
                {
                    System.Threading.Thread.Sleep(200);
                    Manager.BotClient.UnbanChatMemberAsync(message.Chat.Id, userToKick);
                }
            }
            catch
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ParseMode = ParseMode.Markdown,
                       Text = CacheData.GetTranslation("en", "command_kick_error")
                   });
                return;
            }
            
            UserTools.AddPenalty(message.Chat.Id, userToKick,
                Models.TrustFactorLog.TrustFactorAction.kick, Manager.MyId);
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
