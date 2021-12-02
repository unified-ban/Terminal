/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Unifiedban.Models.Translation;

namespace Unifiedban.Terminal.Bot.Command
{
    public class SetWelcome : ICommand
    {
        public void Execute(Message message)
        {
            if (!Utils.BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Basic) &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                return;
            }

            string[] hasMessage = message.Text.Split(" ");
            if (hasMessage.Length > 1)
            {
                bool result = Utils.ConfigTools.UpdateWelcomeText(message.Chat.Id, message.Text.Remove(0, hasMessage[0].Length + 1));
                if (result)
                {
                    Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                }
                return;
            }

            CommandMessage commandMessage = new CommandMessage()
            {
                Command = "SetWelcomeText",
                Value = "",
                Message = message,
                Timestamp = DateTime.UtcNow
            };
            CommandQueueManager.EnqueueMessage(commandMessage);
            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ReplyToMessageId = message.MessageId,
                    ParseMode = ParseMode.Html,
                    Text = $"<b>[ADMIN] [r:{message.MessageId}]</b>\n"
                    + CacheData.GetTranslation(
                        CacheData.Groups[message.Chat.Id].SettingsLanguage,
                        "command_setwelcome_instructions"),
                    ReplyMarkup = new ForceReplyMarkup() { Selective = true }
                });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
