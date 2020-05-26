/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class RemoveSafeGroup : ICommand
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
                        Text = CacheData.GetTranslation("en", "remsafe_command_error_notadmin")
                    });
                return;
            }

            string url = message.Text.Split(" ")[1];

            if (message.Text.Split(" ")[1].StartsWith("@"))
                url = "https://t.me/" + message.Text.Split(" ")[1].Remove(0, 1);

            if (!Controls.Manager.IsTelegramLink(url))
            {
                MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    Text = CacheData.GetTranslation("en", "remsafe_command_error_invalidgroupname")
                });
                return;
            }

            BusinessLogic.Group.SafeGroupLogic safeGroupLogic =
                new BusinessLogic.Group.SafeGroupLogic();
            Models.SystemLog.ErrorCodes removed = safeGroupLogic.Remove(
                CacheData.Groups[message.Chat.Id].GroupId,
                url, -2);
            if (removed == Models.SystemLog.ErrorCodes.Error)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "remsafe_command_error_general")
                    });
                return;
            }

            string confirmationMessage = CacheData.GetTranslation("en", "remsafe_command_success");
            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    Text = confirmationMessage.Replace("{{groupname}}",
                        message.Text.Split(" ")[1].Trim())
                });

            Filters.SafeGroupFilter.LoadCache();
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
