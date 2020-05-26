/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class AddBadWord : ICommand
    {
        public void Execute(Message message)
        {
            if (!Utils.BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Basic) &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return;
            }

            string[] arguments = message.Text.Split(" ");

            if (arguments.Length < 3)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "addbadword_command_error_missingargument")
                    });
                return;
            }

            bool added = Filters.BadWordFilter.BanWord(
                CacheData.Groups[message.Chat.Id].GroupId,
                arguments[1].Trim(),
                message.Text
                    .Substring(arguments[0].Length + arguments[1].Length + 1)
                    .Trim());

            if (!added)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "addbadword_command_error")
                    });
                return;
            }

            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    Text = CacheData.GetTranslation("en", "addbadword_command_success")
                });
            return;
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
