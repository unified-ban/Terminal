/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Feedback : ICommand
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
                        Text = CacheData.GetTranslation("en", "feedback_command_error_notadmin")
                    });
                return;
            }

            List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();
            buttons.Add(new List<InlineKeyboardButton>(){
                InlineKeyboardButton.WithCallbackData(
                        CacheData.GetTranslation("en", "feedback_type_suggestion"),
                        $"/feedback type:suggestion"
                        )
                });
            buttons.Add(new List<InlineKeyboardButton>(){
                InlineKeyboardButton.WithCallbackData(
                        CacheData.GetTranslation("en", "feedback_type_reportBug"),
                        $"/feedback type:reportBug"
                        )
                });
            buttons.Add(new List<InlineKeyboardButton>(){
                InlineKeyboardButton.WithCallbackData(
                        CacheData.GetTranslation("en", "feedback_type_reportUser"),
                        $"/feedback type:reportUser"
                        )
                });

            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ReplyToMessageId = message.MessageId,
                    ParseMode = ParseMode.Markdown,
                    Text = "*[ADMIN]*\nSelect feedback type:",
                    ReplyMarkup = new InlineKeyboardMarkup(
                        buttons
                    )
                });
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            if (!Utils.BotTools.IsUserOperator(callbackQuery.Message.From.Id, Models.Operator.Levels.Basic) &&
                !Utils.ChatTools.IsUserAdmin(callbackQuery.Message.Chat.Id, callbackQuery.Message.From.Id))
            {
                return;
            }

            Manager.BotClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);

            string data = callbackQuery.Data.Replace("/feedback ", "");
            string type = data.Split(':')[1];

            CommandMessage commandMessage = new CommandMessage()
            {
                Command = "Feedback",
                Value = type,
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
                    Text = $"*[ADMIN] [r:{callbackQuery.Message.MessageId}]*\nProvide feedback text:",
                    ReplyMarkup = new ForceReplyMarkup() { Selective = true }
                });
        }
    }
}
