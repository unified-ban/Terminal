/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Get : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            string dataMessage = "*[Report]*\nRequested information:\n\n";
            int userId = 0;
            if (message.ReplyToMessage == null && message.ForwardFromMessageId == 0)
            {
                string[] arguments = message.Text.Split(" ");
                if (arguments.Count() == 2)
                {
                    TryGetFromId(message);
                    return;
                }
                else if (arguments.Count() > 2)
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            ParseMode = ParseMode.Markdown,
                            Text = "*Error:* too many arguments."
                        });
                    return;
                }
                userId = message.From.Id;
                dataMessage += "*Chat Id:* {{chat_id}}\n";
                dataMessage += "*User Id:* {{from_id}}\n";
                dataMessage += "*Username:* {{from_username}}\n";
                dataMessage += "*Is bot:* {{from_isBot}}\n";
            }
            else if (message.ReplyToMessage != null)
            {
                if (message.ReplyToMessage.ForwardFrom != null)
                {
                    userId = message.ReplyToMessage.ForwardFrom.Id;
                    dataMessage += "*Chat Id:* {{replyToMessage_forwardFrom_chat_id}}\n";
                    dataMessage += "*User Id:* {{replyToMessage_forwardFrom_from_id}}\n";
                    dataMessage += "*Username:* {{replyToMessage_forwardFrom_from_username}}\n";
                    dataMessage += "*Is bot:* {{replyToMessage_forwardFrom_from_isBot}}\n";
                }
                else
                {
                    userId = message.ReplyToMessage.From.Id;
                    dataMessage += "*Chat Id:* {{replyToMessage_chat_id}}\n";
                    dataMessage += "*User Id:* {{replyToMessage_from_id}}\n";
                    dataMessage += "*Username:* {{replyToMessage_from_username}}\n";
                    dataMessage += "*Is bot:* {{replyToMessage_from_isBot}}\n";
                }
            }
            else if (message.ForwardFrom != null)
            {
                userId = message.ForwardFrom.Id;
                dataMessage += "*Chat Id:* {{forwardFrom_chat_id}}\n";
                dataMessage += "*User Id:* {{forwardFrom_from_id}}\n";
                dataMessage += "*Username:* {{forwardFrom_from_username}}\n";
                dataMessage += "*Is bot:* {{forwardFrom_from_isBot}}\n";
            }

            int trustPoints = 100;
            if (CacheData.TrustFactors.ContainsKey(userId))
            {
                trustPoints = CacheData.TrustFactors[userId].Points;
            }
            dataMessage += $"*Trust factor:* { trustPoints }/100 { (trustPoints < 71 ? " ⚠️" : "") }\n";
            Models.User.Banned isBlacklisted = CacheData.BannedUsers
                .SingleOrDefault(x => x.TelegramUserId == userId);
            dataMessage += $"*Is blacklisted:* { (isBlacklisted == null ? "no ✅" : "*yes* ❗️")}\n";
            if (isBlacklisted != null)
            {
                dataMessage += $"*Listing reason:* { isBlacklisted.Reason.ToString() }\n";
                dataMessage += $"*Listed on:* { isBlacklisted.UtcDate }\n";
            }
            
            string parsedText = Utils.Parsers.VariablesParser(dataMessage, message);
            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = parsedText
                });
            Manager.BotClient.SendTextMessageAsync(
                chatId: CacheData.ControlChatId,
                parseMode: ParseMode.Markdown,
                text: parsedText + String.Format(
                    "\n*hash_code:* #UB{0}-{1}",
                    message.Chat.Id.ToString().Replace("-",""),
                    Guid.NewGuid())
            );
        }

        private void TryGetFromId(Message message)
        {
            string user = message.Text.Split(" ")[1];
            bool isValidId = int.TryParse(user, out int userId);
            bool isValidUsername = user.StartsWith("@");
            
            if (!isValidId && !isValidUsername)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = "*Error:* invalid user reference. Please provide user id or username."
                    });
                return;
            }

            if (isValidUsername)
            {
                if (!CacheData.Usernames.TryGetValue(user.Remove(0,1), out userId))
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            ParseMode = ParseMode.Markdown,
                            Text = "*Error:* username not in cache. Please try with user id or quote user's message."
                        });
                    return;
                }
            }
            
            string dataMessage = "*[Report]*\nRequested (partial) information:\n\n";
            
            dataMessage += $"*Chat Id:* { message.Chat.Id }\n";
            dataMessage += $"*User Id:* { userId }\n";
            if (isValidUsername)
            {
                dataMessage += $"*Username:* { user.Remove(0,1) }\n";
            }

            int trustPoints = 100;
            if (CacheData.TrustFactors.ContainsKey(userId))
            {
                trustPoints = CacheData.TrustFactors[userId].Points;
            }
            dataMessage += $"*Trust factor:* { trustPoints }/100 { (trustPoints < 71 ? " ⚠️" : "") }\n";
            Models.User.Banned isBlacklisted = CacheData.BannedUsers
                .SingleOrDefault(x => x.TelegramUserId == userId);
            dataMessage += $"*Is blacklisted:* { (isBlacklisted == null ? "no ✅" : "*yes* ❗️")}\n";
            if (isBlacklisted != null)
            {
                dataMessage += $"*Listing reason:* { isBlacklisted.Reason.ToString() }\n";
                dataMessage += $"*Listed on:* { isBlacklisted.UtcDate }\n";
            }
            
            string parsedText = Utils.Parsers.VariablesParser(dataMessage, message);
            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = parsedText
                });
            Manager.BotClient.SendTextMessageAsync(
                chatId: CacheData.ControlChatId,
                parseMode: ParseMode.Markdown,
                text: parsedText + String.Format(
                    "\n*hash_code:* #UB{0}-{1}",
                    message.Chat.Id.ToString().Replace("-",""),
                    Guid.NewGuid())
            );
            
        }
        public void Execute(CallbackQuery callbackQuery) { }
    }
}
