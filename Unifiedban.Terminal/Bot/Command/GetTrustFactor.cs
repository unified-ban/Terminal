/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Bot.Command
{
    public class GetTrustFactor : ICommand
    {
        public void Execute(Message message)
        {
            int userId;

            if (message.ReplyToMessage == null)
            {
                if (!message.Text.Trim().Contains(" "))
                {
                    userId = message.From.Id;
                }
                else if (message.Text.Split(" ")[1].StartsWith("@"))
                {
                    if (!CacheData.Usernames.Keys.Contains(message.Text.Split(" ")[1].Remove(0, 1)))
                    {
                        MessageQueueManager.EnqueueMessage(
                            new Models.ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                Text = CacheData.GetTranslation("en", "gettrustfactor_command_error_invalidUsername")
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
                            new Models.ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                Text = CacheData.GetTranslation("en", "gettrustfactor_command_error_invalidUserId")
                            });
                        return;
                    }
                }
            }
            else
                userId = message.ReplyToMessage.From.Id;

            int points = 100;
            if (CacheData.TrustFactors.ContainsKey(userId))
            {
                points = CacheData.TrustFactors[userId].Points;
            }

            string answerNoUsername = CacheData.GetTranslation("en", "gettrustfactor_command_text");
            string answerWithUsername = CacheData.GetTranslation("en", "gettrustfactor_command_text_wuser");
            string username = CacheData.Usernames.Single(x => x.Value == userId).Key;
            if (String.IsNullOrEmpty(username))
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ReplyToMessageId = message.MessageId,
                        Text = String.Format(answerNoUsername, userId, points)
                    });
                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(answerNoUsername, userId, points));

                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "*[Report]*\n" +
                        "ℹ️Requested trust factor of user.\n" +
                        "\n*Chat:* {0}" +
                        "\n*ChatId:* {1}" +
                        "\n*UserId:* {2}" +
                        "\n*Username:* {3}" +
                        "\n*Trust factor:* {4}/100" + (points < 71 ? " ⚠️" : "") +
                        "\n\n*hash_code:* #UB{5}-{6}",
                        message.Chat.Title,
                        message.Chat.Id,
                        userId,
                        username,
                        points,
                        message.Chat.Id.ToString().Replace("-",""),
                        Guid.NewGuid())
                );
            }
            else
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ReplyToMessageId = message.MessageId,
                        Text = String.Format(answerWithUsername, userId, points, username)
                    });
                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "*[Report]*\n" +
                        "ℹ️ Requested trust factor of user.\n" +
                        "\n*Chat:* {0}" +
                        "\n*ChatId:* {1}" +
                        "\n*UserId:* {2}" +
                        "\n*Trust factor:* {4}/100" + (points < 71 ? " ⚠️" : "") +
                        "\n\n*hash_code:* #UB{5}-{6}",
                        message.Chat.Title,
                        message.Chat.Id,
                        userId,
                        username,
                        points,
                        message.Chat.Id.ToString().Replace("-",""),
                        Guid.NewGuid())
                );
            }
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
