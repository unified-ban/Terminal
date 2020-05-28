﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Hangfire;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Unifiedban.Terminal.Bot;

namespace Unifiedban.Terminal.Utils
{
    public class ChatTools
    {
        public static void Initialize()
        {
            RecurringJob.AddOrUpdate("ChatTools_CheckNightSchedule", () => CheckNightSchedule(), "0 * * ? * *");
            RecurringJob.Trigger("ChatTools_CheckNightSchedule");

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Chat Tools initialized",
                UserId = -2
            });
        }

        public static bool IsUserAdmin(long chatId, long userId)
        {
            var administrators = Bot.Manager.BotClient.GetChatAdministratorsAsync(chatId).Result;
            foreach(Telegram.Bot.Types.ChatMember member in administrators)
            {
                if (member.User.Id == userId)
                    return true;
            }
            return false;
        }

        public static List<int> GetChatAdminIds(long chatId)
        {
            List<int> admins = new List<int>();
            var administrators = Manager.BotClient.GetChatAdministratorsAsync(chatId).Result;
            foreach (ChatMember member in administrators)
            {
                admins.Add(member.User.Id);
            }
            return admins;
        }

        public static bool HandleSupportSessionMsg(Message message)
        {
            if (!CacheData.ActiveSupport
                .Contains(message.Chat.Id))
                return false;

            bool isFromOperator = false;
            if (BotTools.IsUserOperator(message.From.Id))
            {
                isFromOperator = true;
                Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                Models.ChatMessage newMsg = new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    Text = message.Text +
                        "\n\nMessage from operator: " + message.From.Username
                };
                if (message.ReplyToMessage != null)
                    newMsg.ReplyToMessageId = message.ReplyToMessage.MessageId;
                MessageQueueManager.EnqueueMessage(newMsg);
            }

            Task.Run(() => RecordSupportSessionMessage(message));

            return isFromOperator;
        }

        private static void RecordSupportSessionMessage(Message message)
        {
            Models.SupportSessionLog.SenderType senderType = Models.SupportSessionLog.SenderType.User;
            if (BotTools.IsUserOperator(message.From.Id))
                senderType = Models.SupportSessionLog.SenderType.Operator;
            else if (CacheData.CurrentChatAdmins[message.Chat.Id]
                    .Contains(message.From.Id))
                senderType = Models.SupportSessionLog.SenderType.Admin;

            LogTools.AddSupportSessionLog(new Models.SupportSessionLog()
            {
                GroupId = CacheData.Groups[message.Chat.Id].GroupId,
                SenderId = message.From.Id,
                Text = message.Text,
                Timestamp = DateTime.UtcNow,
                Type = senderType
            });
        }

        public static void CheckNightSchedule()
        {
            List<Models.Group.NightSchedule> activeSchedules =
                CacheData.NightSchedules.Values
                    .Where(x => x.State != Models.Group.NightSchedule.Status.Deactivated)
                    .ToList();
            CloseGroups(activeSchedules);
            OpenGroups(activeSchedules);
        }

        private static void CloseGroups(List<Models.Group.NightSchedule> nightSchedules)
        {
            foreach(Models.Group.NightSchedule nightSchedule in nightSchedules)
            {
                if (CacheData.NightSchedules[nightSchedule.GroupId].State == Models.Group.NightSchedule.Status.Active)
                    continue;

                if(nightSchedule.UtcStartDate.Value.TimeOfDay <= DateTime.UtcNow.TimeOfDay)
                {
                    CacheData.NightSchedules[nightSchedule.GroupId].State = Models.Group.NightSchedule.Status.Active;

                    Manager.BotClient.SetChatPermissionsAsync(CacheData.Groups.Values
                        .Single(x => x.GroupId == nightSchedule.GroupId).TelegramChatId,
                        new ChatPermissions()
                        {
                            CanSendMessages = false,
                            CanAddWebPagePreviews = false,
                            CanChangeInfo = false,
                            CanInviteUsers = false,
                            CanPinMessages = false,
                            CanSendMediaMessages = false,
                            CanSendOtherMessages = false,
                            CanSendPolls = false
                        });
                }
            }
        }

        private static void OpenGroups(List<Models.Group.NightSchedule> nightSchedules)
        {
            foreach (Models.Group.NightSchedule nightSchedule in nightSchedules)
            {
                if (CacheData.NightSchedules[nightSchedule.GroupId].State == Models.Group.NightSchedule.Status.Programmed)
                    continue;

                if (nightSchedule.UtcEndDate.Value.TimeOfDay <= DateTime.UtcNow.TimeOfDay)
                {
                    CacheData.NightSchedules[nightSchedule.GroupId].State = Models.Group.NightSchedule.Status.Programmed;

                    Manager.BotClient.SetChatPermissionsAsync(CacheData.Groups.Values
                        .Single(x => x.GroupId == nightSchedule.GroupId).TelegramChatId,
                        new ChatPermissions()
                        {
                            CanSendMessages = true,
                            CanAddWebPagePreviews = true,
                            CanChangeInfo = true,
                            CanInviteUsers = true,
                            CanPinMessages = true,
                            CanSendMediaMessages = true,
                            CanSendOtherMessages = true,
                            CanSendPolls = true
                        });
                }
            }
        }
    }
}
