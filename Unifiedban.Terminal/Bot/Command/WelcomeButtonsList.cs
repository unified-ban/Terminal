/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;
using Unifiedban.Models;

namespace Unifiedban.Terminal.Bot.Command
{
    public class WelcomeButtonsList : ICommand
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

            string btnList = "This is the list of welcome buttons:";
            BusinessLogic.ButtonLogic buttonLogic = new BusinessLogic.ButtonLogic();
            foreach (Button btn in buttonLogic
                .GetByChat(CacheData.Groups[message.Chat.Id]
                .GroupId))
            {
                btnList += Environment.NewLine;
                btnList += "* " + btn.Name + " -> " + btn.Content;
            }

            MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       Text = btnList,
                       DisableWebPagePreview = true
                   });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
