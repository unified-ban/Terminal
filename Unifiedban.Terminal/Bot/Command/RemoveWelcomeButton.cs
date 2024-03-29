﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class RemoveWelcomeButton : ICommand
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

            string[] arguments = message.Text.Split(" ");
            if (arguments.Length < 2)
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "rwb_command_error_invalidsyntax")
                   });
                return;
            }

            BusinessLogic.ButtonLogic buttonLogic = new BusinessLogic.ButtonLogic();
            Models.SystemLog.ErrorCodes removed = buttonLogic.Remove(CacheData.Groups[message.Chat.Id].GroupId,
                message.Text.Remove(0, arguments[0].Length + 1), -2);
            if (removed == Models.SystemLog.ErrorCodes.Error)
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "rwb_command_error_general")
                   });
                return;
            }

            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            string successMsg = CacheData.GetTranslation("en", "rwb_command_success");
            MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       Text = successMsg.Replace("{{wbName}}", arguments[1])
                   });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
