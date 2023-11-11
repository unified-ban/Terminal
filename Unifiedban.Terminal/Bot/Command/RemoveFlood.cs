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
    public class RemoveFlood : ICommand
    {
        public void Execute(Message message){ }

        public void Execute(CallbackQuery callbackQuery)
        {
            if (!Utils.BotTools.IsUserOperator(callbackQuery.From.Id) &&
                !Utils.ChatTools.IsUserAdmin(callbackQuery.Message.Chat.Id, callbackQuery.From.Id))
            {
                return;
            }

            bool parsed = long.TryParse(callbackQuery.Data.Split(" ")[1], out long userId);
            if (!parsed)
                return;

            Manager.BotClient.DeleteMessageAsync(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId);

            Manager.BotClient.RestrictChatMemberAsync(
                    callbackQuery.Message.Chat.Id,
                    userId,
                    new ChatPermissions()
                    {
                        CanSendMessages = true,
                        CanSendAudios = true,
                        CanSendDocuments = true,
                        CanSendPhotos = true,
                        CanSendVideos = true,
                        CanSendVideoNotes = true,
                        CanSendVoiceNotes = true,
                        CanSendPolls = true,
                        CanSendOtherMessages = true,
                        CanAddWebPagePreviews = true,
                        CanChangeInfo = true,
                        CanInviteUsers = true,
                        CanPinMessages = true,
                        CanManageTopics = true
                    });
        }
    }
}
