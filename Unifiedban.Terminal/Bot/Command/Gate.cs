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
    public class Gate : ICommand
    {
        public void Execute(Message message)
        {
            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == message.From.Id
                && x.Level >= Models.Operator.Levels.Basic) == null &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
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
            if (hasMessage.Length == 2)
            {
                if (hasMessage[1] == "close")
                    ToggleGate(message, false);
                else if (hasMessage[1] == "open")
                    ToggleGate(message, true);
                else
                    MessageQueueManager.EnqueueMessage(
                       new ChatMessage()
                       {
                           Timestamp = DateTime.UtcNow,
                           Chat = message.Chat,
                           ReplyToMessageId = message.MessageId,
                           Text = CacheData.GetTranslation("en", "error_gate_command_argument")
                       });
                return;
            }
        }

        public void Execute(CallbackQuery callbackQuery) { }

        public static void ToggleGate(Message message, bool newStatus)
        {
            Models.Group.ConfigurationParameter config = CacheData.GroupConfigs[message.Chat.Id]
               .Where(x => x.ConfigurationParameterId == "Gate")
               .SingleOrDefault();
            if (config == null)
                return;

            CacheData.GroupConfigs[message.Chat.Id]
                [CacheData.GroupConfigs[message.Chat.Id]
                .IndexOf(config)]
                .Value = newStatus ? "true" : "false";

            Manager.BotClient.SetChatPermissionsAsync(message.Chat.Id,
                new ChatPermissions()
                {
                    CanSendMessages = newStatus,
                    CanAddWebPagePreviews = newStatus,
                    CanChangeInfo = newStatus,
                    CanInviteUsers = newStatus,
                    CanPinMessages = newStatus,
                    CanSendMediaMessages = newStatus,
                    CanSendOtherMessages = newStatus,
                    CanSendPolls = newStatus
                });

            string status = newStatus ? "open" : "closed";
            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    Text = $"The group is now {status}"
                });
        }
    }
}
