/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Unifiedban.Models;

namespace Unifiedban.Terminal.Bot.Command
{
    public class EndSupport : ICommand
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
                        Text = CacheData.GetTranslation("en", "feedback_command_error_notoperator")
                    });
                return;
            }

            if (CacheData.ActiveSupport.Contains(message.Chat.Id))
            {
                CacheData.ActiveSupport.Remove(message.Chat.Id);
                CacheData.CurrentChatAdmins.Remove(message.Chat.Id);

                Manager.BotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "Operator *{0}* ended the support session.",
                        message.From.Username)
                );
                MessageQueueManager.EnqueueLog(new ChatMessage()
                {
                    ParseMode = ParseMode.Markdown,
                    Text = String.Format(
                        "*[Log]*" +
                        "Support session ended by operator *{0}*" +
                        "\nChatId: `{1}`" +
                        "\nChat: `{2}`" +
                        "\nUserId: `{3}`" +
                        "\n\n*hash_code:* #UB{4}-{5}",
                        message.From.Username,
                        message.Chat.Id,
                        message.Chat.Title,
                        message.From.Id,
                        message.Chat.Id.ToString().Replace("-", ""),
                        Guid.NewGuid())
                });
            }
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
