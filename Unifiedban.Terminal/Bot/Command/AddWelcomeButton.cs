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
    public class AddWelcomeButton : ICommand
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
            if (arguments.Length < 3)
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "awb_command_error_invalidsyntax")
                   });
                return;
            }

            string text = message.Text
                .Replace(arguments[0], "")
                .Replace(arguments[arguments.Length - 1], "")
                .Trim();
            string url = arguments[arguments.Length - 1];

            if (!Utils.BotTools.IsValidUrl(url))
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "awb_command_error_invalidsyntax")
                   });
                return;
            }

            BusinessLogic.ButtonLogic buttonLogic = new BusinessLogic.ButtonLogic();
            Models.Button newBtn = buttonLogic.Add(CacheData.Groups[message.Chat.Id].GroupId,
                text, url, Models.Button.Scopes.Welcome, -2);
            if (newBtn == null)
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "awb_command_error_general")
                   });
                return;
            }

            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            string successMsg = CacheData.GetTranslation("en", "awb_command_success");
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
