/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot.Command
{
    public class TestCommand : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            if (!Utils.BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Super))
            {
                return;
            }

            string[] data = message.Text.Split(" ");
            if (data.Length >= 2)
            {
                handleRequest(data, message);
                return;
            }

            List<InlineKeyboardButton> confirmationButton = new List<InlineKeyboardButton>();
            confirmationButton.Add(InlineKeyboardButton.WithUrl("Start with command", "http://t.me/LinuxPixelHubBot?start=motd"));
            confirmationButton.Add(InlineKeyboardButton.WithCallbackData(
                                        CacheData.GetTranslation("en", "captcha_iamhuman", true),
                                        $"/test1 " + message.From.Id
                                        ));
            confirmationButton.Add(InlineKeyboardButton.WithCallbackData(
                                        "RM",
                                        $"/test1 rm"
                                        ));
            confirmationButton.Add(InlineKeyboardButton.WithCallbackData(
                                        "Switch invalid command" + (CacheData.AnswerInvalidCommand ? " ✅" : " ❌"),
                                        $"/test1 switchinvalidcommand"
                                        ));

            MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = "*[ADMIN]*\nSelect a test",
                        ReplyMarkup = new InlineKeyboardMarkup(
                            confirmationButton
                        )
                    });
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            if (!Utils.BotTools.IsUserOperator(callbackQuery.From.Id, Models.Operator.Levels.Super))
            {
                return;
            }

            string[] data = callbackQuery.Data.Split(" ");
            if (data.Length < 2)
                return;

            handleRequest(data, callbackQuery.Message);
        }

        private void handleRequest(string[] data, Message message)
        {
            switch (data[1])
            {
                case "rm":
                    if (data.Length == 3)
                        deleteLastMessages(message, Convert.ToInt32(data[2]));
                    else
                        deleteLastMessages(message);
                    break;
                case "switchinvalidcommand":
                        CacheData.AnswerInvalidCommand = !CacheData.AnswerInvalidCommand;
                    break;
                default:
                    break;
            }
        }

        private void deleteLastMessages(Message message, int amount = 1)
        {
            int startMessage = message.ReplyToMessage != null ? message.ReplyToMessage.MessageId : message.MessageId - 1;
            int nextMessage = message.ReplyToMessage != null ? message.ReplyToMessage.MessageId : message.MessageId - 1;

            while (nextMessage >= startMessage - amount)
            {
                Manager.BotClient.DeleteMessageAsync(message.Chat.Id, nextMessage);
                nextMessage--;
            }
        }
    }
}
