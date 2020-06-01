/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot.Command
{
    public class RemoveFromBlacklist : ICommand
    {
        public void Execute(Message message)
        {
            if (!Utils.BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Basic))
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "white_command_error_notoperator")
                    });
                return;
            }

            int userToUnban;

            if (message.ReplyToMessage == null)
            {
                if (message.Text.Split(" ")[1].StartsWith("@"))
                {
                    if (!CacheData.Usernames.Keys.Contains(message.Text.Split(" ")[1].Remove(0, 1)))
                    {
                        MessageQueueManager.EnqueueMessage(
                            new Models.ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                Text = CacheData.GetTranslation("en", "white_command_error_invalidUsername")
                            });
                        return;
                    }
                    userToUnban = CacheData.Usernames[message.Text.Split(" ")[1].Remove(0, 1)];
                }
                else
                {
                    bool isValid = int.TryParse(message.Text.Split(" ")[1], out userToUnban);
                    if (!isValid)
                    {
                        MessageQueueManager.EnqueueMessage(
                            new Models.ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                Text = CacheData.GetTranslation("en", "white_command_error_invalidUserId")
                            });
                        return;
                    }
                }
            }
            else
                userToUnban = message.ReplyToMessage.From.Id;
            
            Utils.UserTools.RemoveUserFromBlacklist(message, userToUnban);
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
