/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Check : ICommand
    {
        public void Execute(Message message)
        {
            if (!Utils.BotTools.IsUserOperator(message.From.Id) &&
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

            bool canRestrictMembers = false;
            bool canDeleteMessages = false;
            bool canPinMessages = false;
            string text = CacheData.SysConfigs
                .Single(x => x.SysConfigId == "CommandCheckKoText").Value;

            var me = Manager.BotClient.GetChatMemberAsync(message.Chat.Id, Manager.MyId).Result;
            if (me is ChatMemberAdministrator chatMemberAdministrator)
            {
                canRestrictMembers = chatMemberAdministrator.CanRestrictMembers;
                canDeleteMessages = chatMemberAdministrator.CanDeleteMessages;
                canPinMessages = chatMemberAdministrator.CanPinMessages ?? false;
            }

            if (canRestrictMembers && canDeleteMessages)
                text = CacheData.SysConfigs.Single(x => x.SysConfigId == "CommandCheckOkText").Value;
            if(canDeleteMessages)
                Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            text = text.Replace("{{has_ban_users}}", canRestrictMembers ? "{{true}}" : "{{false}}");
            text = text.Replace("{{has_delete_messages}}", canDeleteMessages ? "{{true}}" : "{{false}}");
            text = text.Replace("{{has_pin_messages}}", canPinMessages ? "{{true}}" : "{{false}}");

            text = Utils.Parsers.VariablesParser(text); // TODO select group's settings language

            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = text
                });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
