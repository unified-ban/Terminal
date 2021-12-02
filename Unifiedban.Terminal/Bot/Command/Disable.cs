/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Disable : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            if (!Utils.BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Advanced))
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "User *{0}:{1}* tried to use command Disable!",
                        message.From.Id,
                        message.From.Username)
                );
                return;
            }

            long chatId = 0;

            string[] data = message.Text.Split(" ");
            if (data.Length >= 2)
            {
                bool isInt = long.TryParse(data[1], out chatId);
                if (!isInt)
                {
                    Manager.BotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        parseMode: ParseMode.Markdown,
                        text: "The provided chatId is not a number.\n"
                            + "**Syntax:**\n/disable (current group)\n/disable {chatId}"
                        );
                    return;
                }
            }
            else
                chatId = message.Chat.Id;

            CacheData.Groups[message.Chat.Id].State = Models.Group.TelegramGroup.Status.Inactive;

            MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       Text = CacheData.GetTranslation("en", "command_disable_successful")
                   });
            Manager.BotClient.SendTextMessageAsync(
                chatId: CacheData.ControlChatId,
                parseMode: ParseMode.Markdown,
                text: String.Format(
                    "Operator *{0}* has disabled group {1}:{2}",
                    message.From.Id,
                    message.Chat.Id, message.Chat.Title)
            );
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
