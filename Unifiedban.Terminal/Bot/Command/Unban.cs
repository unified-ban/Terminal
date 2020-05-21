/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Unban : ICommand
    {
        public void Execute(Message message)
        {
            if (!Utils.BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Basic) &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "unban_command_error_notadmin")
                    });
                return;
            }

            if (Manager.BotClient.GetChatAdministratorsAsync(message.Chat.Id).Result
                .Single(x => x.User.Id == message.From.Id)
                .CanRestrictMembers == false)
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "unban_command_error_adminPrivilege")
                    });
                return;
            }

            if (message.Chat.Type != ChatType.Supergroup)
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "unban_command_notSuperGroup")
                    });
                return;
            }

            int userId;

            if (message.ReplyToMessage == null)
            {
                if (message.Text.Split(" ")[1].StartsWith("@"))
                {
                    if (!CacheData.Usernames.Keys.Contains(message.Text.Split(" ")[1].Remove(0, 1)))
                    {
                        MessageQueueManager.EnqueueMessage(
                            new ChatMessage()
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
                    bool isValid = int.TryParse(message.Text.Split(" ")[1], out userId);
                    if (!isValid)
                    {
                        MessageQueueManager.EnqueueMessage(
                            new ChatMessage()
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
                userId = message.ReplyToMessage.From.Id;

            try
            {
                Manager.BotClient.UnbanChatMemberAsync(message.Chat.Id, userId);
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "unban_command_success")
                    });
            }
            catch
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
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
