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
using Unifiedban.Models.Translation;

namespace Unifiedban.Terminal.Bot.Command
{
    public class SetRules : ICommand
    {
        public void Execute(Message message)
        {
            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == message.From.Id
                && x.Level >= Models.Operator.Levels.Basic) == null)
            {
                MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                return;
            }

            string[] hasMessage = message.Text.Split(" ");
            if(hasMessage.Length > 1)
            {
                Utils.ConfigTools.UpdateRulesText(message.Chat.Id, message.Text.Remove(0, hasMessage[0].Length + 1));
                return;
            }

            CommandMessage commandMessage = new CommandMessage()
            {
                Command = "SetRulesText",
                Value = "",
                Message = message,
                Timestamp = DateTime.UtcNow
            };
            CommandQueueManager.EnqueueMessage(commandMessage);
            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ReplyToMessageId = message.MessageId,
                    ParseMode = ParseMode.Html,
                    Text = $"<b>[ADMIN] [r:{message.MessageId}]</b>\nProvide new rules message.\n\n"
                    + "<b>Available variables:</b> {{from_username}}, {{from_first_name}}, {{from_last_name}}, {{chat_title}}.\n\n"
                    + "<b>Available markup:</b> HTML with \\n as line break.",
                    ReplyMarkup = new ForceReplyMarkup() { Selective = true }
                });
        }

        public void Execute(CallbackQuery callbackQuery) {}
    }
}
