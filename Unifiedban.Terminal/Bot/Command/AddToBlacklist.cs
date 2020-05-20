﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot.Command
{
    public class AddToBlacklist : ICommand
    {
        public void Execute(Message message)
        {
            if (!Utils.BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Advanced))
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "bb_command_error_notoperator")
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
                                Text = CacheData.GetTranslation("en", "bb_command_error_invalidUsername")
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
                                Text = CacheData.GetTranslation("en", "bb_command_error_invalidUserId")
                            });
                        return;
                    }
                }
            }
            else
                userToBan = message.ReplyToMessage.From.Id;

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
                new ChatMessage()
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
                Models.Operator.Levels.Advanced))
            {
                return;
            }

            string[] arguments = callbackQuery.Data.Split(" ");
            int userToBan = Convert.ToInt32(arguments[1]);
            Models.User.Banned.BanReasons reason = Models.User.Banned.BanReasons.Other;

            switch (arguments[2])
            {
                case "spam":
                    reason = Models.User.Banned.BanReasons.Spam;
                    break;
                case "scam":
                    reason = Models.User.Banned.BanReasons.Scam;
                    break;
                case "harassment":
                    reason = Models.User.Banned.BanReasons.Harassment;
                    break;
                case "other":
                    reason = Models.User.Banned.BanReasons.Other;
                    break;
            }

            if (reason == Models.User.Banned.BanReasons.Other)
            {
                CommandMessage commandMessage = new CommandMessage()
                {
                    Command = "AddUserToBlacklist",
                    Value = userToBan.ToString(),
                    Message = callbackQuery.Message,
                    Timestamp = DateTime.UtcNow
                };
                CommandQueueManager.EnqueueMessage(commandMessage);

                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
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
                AddUserToBlacklist(callbackQuery.Message, userToBan, reason, null);
        }

        public static void AddUserToBlacklist(Message message,
            int userToBan, Models.User.Banned.BanReasons reason,
            string otherReason)
        {
            try
            {
                Manager.BotClient.KickChatMemberAsync(message.Chat.Id, userToBan);
                

                BusinessLogic.User.BannedLogic bl = new BusinessLogic.User.BannedLogic();
                Models.User.Banned banned = bl.Add(userToBan, reason, -2);
                if(banned == null)
                {
                    MessageQueueManager.EnqueueMessage(
                        new ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            Text = CacheData.GetTranslation("en", "bb_command_success")
                        });

                    CacheData.BannedUsers.Add(banned);

                    Manager.BotClient.SendTextMessageAsync(
                        chatId: CacheData.ControlChatId,
                        parseMode: ParseMode.Markdown,
                        text: String.Format(
                            "User *{0}* black listed for reason {1}:{2}.",
                            userToBan, reason, otherReason)
                    );
                }

                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "bb_command_error")
                    });
            }
            catch
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "bb_command_error")
                    });
            }
        }
    }
}