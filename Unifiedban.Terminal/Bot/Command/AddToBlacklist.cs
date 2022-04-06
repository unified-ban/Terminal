/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot.Command
{
    public class AddToBlacklist : ICommand
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
                        Text = CacheData.GetTranslation("en", "bb_command_error_notoperator")
                    });
                return;
            }

            long userToBan;

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
                                Text = CacheData.GetTranslation("en", "bb_command_error_invalidUsername")
                            });
                        return;
                    }
                    userToBan = CacheData.Usernames[message.Text.Split(" ")[1].Remove(0, 1)];
                }
                else
                {
                    bool isValid = long.TryParse(message.Text.Split(" ")[1], out userToBan);
                    if (!isValid)
                    {
                        MessageQueueManager.EnqueueMessage(
                            new Models.ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                Text = CacheData.GetTranslation("en", "bb_command_error_invalidUserId")
                            });
                        return;
                    }
                }
            }
            else
                userToBan = message.ReplyToMessage.From.Id;

            if (userToBan == 777000) // Telegram's official updateServiceNotification
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

            List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();
            buttons.Add(new List<InlineKeyboardButton>(){
                InlineKeyboardButton.WithCallbackData(
                        CacheData.GetTranslation("en", "bb_reason_spam"),
                        $"/bb {userToBan} spam"
                        )
                });
            buttons.Add(new List<InlineKeyboardButton>(){
                InlineKeyboardButton.WithCallbackData(
                        CacheData.GetTranslation("en", "bb_reason_scam"),
                        $"/bb {userToBan} scam"
                        )
                });
            buttons.Add(new List<InlineKeyboardButton>(){
                InlineKeyboardButton.WithCallbackData(
                        CacheData.GetTranslation("en", "bb_reason_harassment"),
                        $"/bb {userToBan} harassment"
                        )
                });
            buttons.Add(new List<InlineKeyboardButton>(){
                InlineKeyboardButton.WithCallbackData(
                        CacheData.GetTranslation("en", "bb_reason_other"),
                        $"/bb {userToBan} other"
                        )
                });

            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ReplyToMessageId = message.MessageId,
                    ParseMode = ParseMode.Markdown,
                    Text = "*[OPERATOR]*\nSelect ban reason:",
                    ReplyMarkup = new InlineKeyboardMarkup(buttons)
                });
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            if (!Utils.BotTools.IsUserOperator(callbackQuery.From.Id,
                Models.Operator.Levels.Basic))
            {
                return;
            }

            var arguments = callbackQuery.Data.Split(" ");
            var userToBan = Convert.ToInt64(arguments[1]);

            var reason = arguments[2] switch
            {
                "spam" => Models.User.Banned.BanReasons.Spam,
                "scam" => Models.User.Banned.BanReasons.Scam,
                "harassment" => Models.User.Banned.BanReasons.Harassment,
                "other" => Models.User.Banned.BanReasons.Other,
                _ => Models.User.Banned.BanReasons.Other
            };

            if (reason == Models.User.Banned.BanReasons.Other)
            {
                var commandMessage = new CommandMessage()
                {
                    Command = "AddUserToBlacklist",
                    Value = userToBan.ToString(),
                    Message = callbackQuery.Message,
                    Timestamp = DateTime.UtcNow
                };
                CommandQueueManager.EnqueueMessage(commandMessage);

                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = callbackQuery.Message.Chat,
                        ReplyToMessageId = callbackQuery.Message.ReplyToMessage.MessageId,
                        ParseMode = ParseMode.Markdown,
                        Text = $"*[OPERATOR] [r:{callbackQuery.Message.MessageId}]*\nProvide reason:",
                        ReplyMarkup = new ForceReplyMarkup() { Selective = true }
                    });
            }
            else
                Utils.UserTools.AddUserToBlacklist(callbackQuery.From.Id, callbackQuery.Message, userToBan, reason, null);
        }
    }
}
