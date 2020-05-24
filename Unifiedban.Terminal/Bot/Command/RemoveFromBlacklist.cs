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
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "white_command_error_notoperator")
                    });
                return;
            }

            int userToBan;

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
                                Text = CacheData.GetTranslation("en", "white_command_error_invalidUsername")
                            });
                        return;
                    }
                    userToBan = CacheData.Usernames[message.Text.Split(" ")[1].Remove(0, 1)];
                }
                else
                {
                    bool isValid = int.TryParse(message.Text.Split(" ")[1], out userToBan);
                    if (!isValid)
                    {
                        MessageQueueManager.EnqueueMessage(
                            new ChatMessage()
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
                userToBan = message.ReplyToMessage.From.Id;

            BusinessLogic.User.BannedLogic bl = new BusinessLogic.User.BannedLogic();
            Models.SystemLog.ErrorCodes removed = bl.Remove(userToBan, -2);
            CacheData.BannedUsers.RemoveAll(x => x.TelegramUserId == userToBan);

            if(removed == Models.SystemLog.ErrorCodes.Error)
            {
                MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = String.Format(
                        "Error removing User *{0}* from blacklist.", userToBan)
                });

                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "*[Report]*\n" +
                        "Error removing user `{1}` from blacklist.\n" +
                        "Operator: {0}" +
                        "\n\n*hash_code:* #UB{2}-{3}",
                        message.From.Id,
                        userToBan,
                        message.Chat.Id.ToString().Replace("-", ""),
                        Guid.NewGuid())
                );
            }
            else
            {
                MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = String.Format(
                        "User *{0}* removed from blacklist.", userToBan)
                });

                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "*[Report]*\n" +
                        "Operator `{0}` removed user `{1}` from blacklist.\n" +
                        "\n\n*hash_code:* #UB{2}-{3}",
                        message.From.Id,
                        userToBan,
                        message.Chat.Id.ToString().Replace("-",""),
                        Guid.NewGuid())
                );
            }
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
