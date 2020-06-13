/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Unifiedban.Models;
using Unifiedban.Terminal.Bot;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Controls
{
    public class FloodControl : IControl
    {
        static ConcurrentDictionary<long, Dictionary<int, Flood>> FloodCounter =
            new ConcurrentDictionary<long, Dictionary<int, Flood>>();
        static ConcurrentDictionary<int, DateTime> limitedUsers =
            new ConcurrentDictionary<int, DateTime>();

        struct Flood
        {
            public int UserId { get; set; }
            public int Messages { get; set; }
            public DateTime LastMessage { get; set; }
        }

        public ControlResult DoCheck(Message message)
        {
            if (ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return new ControlResult()
                {
                    CheckName = "Anti Flood",
                    Result = IControl.ControlResultType.skipped
                };
            }

            if(message.Date < DateTime.UtcNow.AddMinutes(-1))
                return new ControlResult()
                {
                    CheckName = "Anti Flood",
                    Result = IControl.ControlResultType.skipped
                };

            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "FloodControl")
                .SingleOrDefault();
            if (configValue != null)
                if (configValue.Value == "false")
                    return new ControlResult()
                    {
                        CheckName = "Anti Flood",
                        Result = IControl.ControlResultType.skipped
                    };

            Dictionary<int, Flood> floodCounter = FloodCounter.GetValueOrDefault(message.Chat.Id);
            if (floodCounter == null)
                FloodCounter.TryAdd(message.Chat.Id, new Dictionary<int, Flood>());

            if (!FloodCounter[message.Chat.Id].TryGetValue(message.From.Id, out Flood currentValue))
                FloodCounter[message.Chat.Id].Add(message.From.Id, new Flood()
                {
                    UserId = message.From.Id,
                    Messages = 1,
                    LastMessage = DateTime.UtcNow
                });
            else
            {
                if(currentValue.LastMessage < DateTime.UtcNow.AddSeconds(-3))
                {
                    FloodCounter[message.Chat.Id][message.From.Id] = new Flood()
                    {
                        UserId = message.From.Id,
                        Messages = 1,
                        LastMessage = DateTime.UtcNow
                    };
                    return new ControlResult()
                    {
                        CheckName = "Anti Flood",
                        Result = IControl.ControlResultType.negative
                    };
                }

                currentValue.Messages += 1;
                currentValue.LastMessage = DateTime.UtcNow;
                FloodCounter[message.Chat.Id][message.From.Id] = currentValue;
            }

            if (FloodCounter[message.Chat.Id][message.From.Id].Messages >= 5)
            {
                int minutes = 5;

                SysConfig floodBanMinutes = CacheData.SysConfigs.Where(x => x.SysConfigId == "FloodBanInMinutes")
                    .SingleOrDefault();
                if (floodBanMinutes != null)
                    int.TryParse(floodBanMinutes.Value, out minutes);
                
                if (limitedUsers.ContainsKey(message.From.Id))
                {
                    if (limitedUsers[message.From.Id] > DateTime.UtcNow.AddMinutes(-minutes))
                    {
                        goto skipLimitAndPenality;
                    }
                }

                limitedUsers.AddOrUpdate(message.From.Id, DateTime.UtcNow, 
                    (key, value) => value = DateTime.UtcNow);

                Bot.Manager.BotClient.RestrictChatMemberAsync(
                                message.Chat.Id,
                                message.From.Id,
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
                                },
                                DateTime.UtcNow.AddMinutes(minutes)
                            ).Wait();
                
                string author = message.From.Username == null
                    ? message.From.FirstName + " " + message.From.LastName
                    : "@" + message.From.Username;
                MessageQueueManager.EnqueueLog(new ChatMessage()
                {
                    ParseMode = ParseMode.Markdown,
                    Text = String.Format(
                        "*[Report]*\n" +
                        "User muted for {0} minutes due to flood.\n" +
                        "\nChat: `{1}`" +
                        "\nAuthor: `{3}`" +
                        "\nUserId: `{2}`" +
                        "\n\n*hash_code:* #UB{4}-{5}",
                        minutes,
                        message.Chat.Title,
                        message.From.Id,
                        author,
                        message.Chat.Id.ToString().Replace("-",""),
                        Guid.NewGuid())
                });

                UserTools.AddPenalty(message.Chat.Id, message.From.Id,
                    TrustFactorLog.TrustFactorAction.limit, Bot.Manager.MyId);

                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = $"User {message.From.Username} has been limited for {minutes} minutes due to flood.\n"
                        + "An admin can immediately remove this limitation by clicking the button below.",
                        ReplyMarkup = new InlineKeyboardMarkup(
                            InlineKeyboardButton.WithCallbackData(
                                CacheData.GetTranslation("en", "button_removeFlood", true),
                                $"/RemoveFlood " + message.From.Id
                                )
                        )
                    });

                skipLimitAndPenality:
                return new ControlResult()
                {
                    CheckName = "Anti Flood",
                    Result = IControl.ControlResultType.positive
                };
            }
            return new ControlResult()
            {
                CheckName = "Anti Flood",
                Result = IControl.ControlResultType.negative
            };
        }
    }
}
