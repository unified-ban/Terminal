/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Ban : ICommand
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
                        Text = CacheData.GetTranslation("en", "ban_command_error_notadmin")
                    });
                return;
            }

            if (Manager.BotClient.GetChatAdministratorsAsync(message.Chat.Id).Result
                .Single(x => x.User.Id == message.From.Id)
                .CanRestrictMembers == false)
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "ban_command_error_adminPrivilege")
                    });
                return;
            }

            int userToKick;

            if (message.ReplyToMessage == null)
            {
                if (message.Text.Split(" ")[1].StartsWith("@"))
                {
                    if(!CacheData.Usernames.Keys.Contains(message.Text.Split(" ")[1].Remove(0, 1)))
                    {
                        MessageQueueManager.EnqueueMessage(
                            new ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                Text = CacheData.GetTranslation("en", "ban_command_error_invalidUsername")
                            });
                        return;
                    }
                    userToKick = CacheData.Usernames[message.Text.Split(" ")[1].Remove(0, 1)];
                }
                else
                {
                    bool isValid = int.TryParse(message.Text.Split(" ")[1], out userToKick);
                    if (!isValid)
                    {
                        MessageQueueManager.EnqueueMessage(
                            new ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                Text = CacheData.GetTranslation("en", "ban_command_error_invalidUserId")
                            });
                        return;
                    }
                }
            }
            else
                userToKick = message.ReplyToMessage.From.Id;

            try
            {
                Manager.BotClient.KickChatMemberAsync(message.Chat.Id, userToKick,
                    DateTime.UtcNow.AddMinutes(-5));
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "ban_command_success")
                    });
                UserTools.AddPenality(userToKick,
                    Models.TrustFactorLog.TrustFactorAction.ban, Manager.MyId);
            }
            catch
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "ban_command_error")
                    });
            }
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
