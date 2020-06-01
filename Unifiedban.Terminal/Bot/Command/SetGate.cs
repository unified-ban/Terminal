/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot.Command
{
    public class SetGate : ICommand
    {
        BusinessLogic.Group.NightScheduleLogic nsl = 
            new BusinessLogic.Group.NightScheduleLogic();

        public void Execute(Message message)
        {
            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == message.From.Id
                && x.Level >= Models.Operator.Levels.Basic) == null &&
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

            SendKeyboard(message, "open");
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            Message message = callbackQuery.Message;
            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == callbackQuery.From.Id
                && x.Level >= Models.Operator.Levels.Basic) == null &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, callbackQuery.From.Id))
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

            string[] parameters = callbackQuery.Data.Split(" ");
            switch (parameters[1])
            {
                case "prevPage":
                case "nextPage":
                    SendKeyboard(message, parameters[3], Convert.ToInt32(parameters[2]));
                    break;
                case "open":
                    SetOpenTime(message, parameters[2]);
                    break;
                case "close":
                    SetCloseTime(message, parameters[2]);
                    break;
            }
        }

        private void SendKeyboard(Message message, string action, int index = 3)
        { 
            DateTime midnight = DateTime.UtcNow;
            midnight = midnight.AddHours(-midnight.Hour);
            midnight = midnight.AddMinutes(-midnight.Minute);
            midnight = midnight.AddSeconds(-midnight.Second);
            DateTime minTime = midnight.AddHours((index * 3) - 3);

            List<List<InlineKeyboardButton>> timesList = new List<List<InlineKeyboardButton>>();

            timesList.Add(new List<InlineKeyboardButton>());
            timesList[0].Add(InlineKeyboardButton.WithCallbackData(
                                minTime.ToString("HH:mm"),
                                $"/setgate {action} {minTime.Hour}:{minTime.Minute}"
                                ));
            minTime = minTime.AddMinutes(30);
            timesList[0].Add(InlineKeyboardButton.WithCallbackData(
                                minTime.ToString("HH:mm"),
                                $"/setgate {action} {minTime.Hour}:{minTime.Minute}"
                                ));

            timesList.Add(new List<InlineKeyboardButton>());
            minTime = minTime.AddMinutes(30);
            timesList[1].Add(InlineKeyboardButton.WithCallbackData(
                                minTime.ToString("HH:mm"),
                                $"/setgate {action} {minTime.Hour}:{minTime.Minute}"
                                ));
            minTime = minTime.AddMinutes(30);
            timesList[1].Add(InlineKeyboardButton.WithCallbackData(
                                minTime.ToString("HH:mm"),
                                $"/setgate {action} {minTime.Hour}:{minTime.Minute}"
                                ));

            timesList.Add(new List<InlineKeyboardButton>());
            minTime = minTime.AddMinutes(30);
            timesList[2].Add(InlineKeyboardButton.WithCallbackData(
                                minTime.ToString("HH:mm"),
                                $"/setgate {action} {minTime.Hour}:{minTime.Minute}"
                                ));
            minTime = minTime.AddMinutes(30);
            timesList[2].Add(InlineKeyboardButton.WithCallbackData(
                                minTime.ToString("HH:mm"),
                                $"/setgate {action} {minTime.Hour}:{minTime.Minute}"
                                ));

            timesList.Add(new List<InlineKeyboardButton>());
            timesList[3].Add(InlineKeyboardButton.WithCallbackData(
                                $"◀️",
                                $"/setgate prevPage {index - 1} {action}"
                                ));
            timesList[3].Add(InlineKeyboardButton.WithCallbackData(
                                $"➡️",
                                $"/setgate nextPage {index + 1} {action}"
                                ));
            if(message.From.Id != Manager.MyId)
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ReplyToMessageId = message.MessageId,
                        ParseMode = ParseMode.Markdown,
                        Text = $"*[ADMIN]*\nSelect at what time (in 24h format) you want to {action} the group.\n\n" +
                        $"⚠️ my reference time is { DateTime.UtcNow.ToString(@"HH:MM") }, " +
                        $"I kindly ask you to consider the timezone difference during selection.",
                        ReplyMarkup = new InlineKeyboardMarkup(
                            timesList
                        )
                    });
            else
                Manager.BotClient.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId,
                    new InlineKeyboardMarkup(timesList));
        }

        private void SetOpenTime(Message message, string time)
        {
            string groupId = CacheData.Groups[message.Chat.Id].GroupId;
            DateTime endTime = new DateTime(
                DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 
                Convert.ToInt32(time.Split(":")[0]), Convert.ToInt32(time.Split(":")[1]), 0);
            if(endTime < DateTime.UtcNow)
            {
                endTime.AddDays(1);
            }
            if (CacheData.NightSchedules.ContainsKey(groupId))
            {
                if (CacheData.NightSchedules[groupId].UtcStartDate.HasValue)
                    if (CacheData.NightSchedules[groupId].UtcStartDate.Value.Hour == endTime.Hour)
                    {
                        MessageQueueManager.EnqueueMessage(
                            new Models.ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                ParseMode = ParseMode.Markdown,
                                Text = "Opening and closing time matches the same hour. Please select a different time.",
                                PostSentAction = Models.ChatMessage.PostSentActions.Destroy,
                                AutoDestroyTimeInSeconds = 5
                            });
                        return;
                    }
                nsl.Update(groupId, Models.Group.NightSchedule.Status.Deactivated,
                    CacheData.NightSchedules[groupId].UtcStartDate, endTime, -2);
            }
            else
            {
                nsl.Add(groupId, Models.Group.NightSchedule.Status.Deactivated,
                    null, endTime, -2);
            }

            CacheData.NightSchedules[groupId] = nsl.GetByChat(groupId);
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            SendKeyboard(message.ReplyToMessage, "close");
        }
        private void SetCloseTime(Message message, string time)
        {
            string groupId = CacheData.Groups[message.Chat.Id].GroupId;
            DateTime startTime = new DateTime(
                DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                Convert.ToInt32(time.Split(":")[0]), Convert.ToInt32(time.Split(":")[1]), 0);
            if (startTime < DateTime.UtcNow)
            {
                startTime.AddDays(1);
            }
            if (CacheData.NightSchedules.ContainsKey(groupId))
            {
                if(CacheData.NightSchedules[groupId].UtcEndDate.HasValue)
                    if (CacheData.NightSchedules[groupId].UtcEndDate.Value.Hour == startTime.Hour)
                    {
                        MessageQueueManager.EnqueueMessage(
                            new Models.ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                ParseMode = ParseMode.Markdown,
                                Text = "Opening and closing time matches the same hour. Please select a different time.",
                                PostSentAction = Models.ChatMessage.PostSentActions.Destroy,
                                AutoDestroyTimeInSeconds = 5
                            });
                        return;
                    }
                nsl.Update(groupId, Models.Group.NightSchedule.Status.Active,
                    startTime, CacheData.NightSchedules[groupId].UtcEndDate, - 2);
            }
            else
            {
                nsl.Add(groupId, Models.Group.NightSchedule.Status.Active,
                    startTime, null, -2);
            }

            CacheData.NightSchedules[groupId] = nsl.GetByChat(groupId);
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = "Congratulations! The night schedule has been updated!"
                });

            Models.Group.ConfigurationParameter config = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "GateSchedule")
                .SingleOrDefault();
            if (config == null)
                return;
            CacheData.GroupConfigs[message.Chat.Id]
                [CacheData.GroupConfigs[message.Chat.Id]
                .IndexOf(config)]
                .Value = "true";
        }
    }
}
