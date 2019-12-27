using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Controls
{
    public class FloodControl : IControl
    {
        static ConcurrentDictionary<long, Dictionary<int, Flood>> FloodCounter =
            new ConcurrentDictionary<long, Dictionary<int, Flood>>();

        struct Flood
        {
            public int UserId { get; set; }
            public int Messages { get; set; }
            public DateTime LastMessage { get; set; }
        }

        public ControlResult DoCheck(Message message)
        {
            if (Utils.BotTools.IsUserOperator(message.From.Id) ||
                Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return new ControlResult()
                {
                    CheckName = "AntiFlood",
                    Result = IControl.ControlResultType.skipped
                };
            }

            if(message.Date < DateTime.UtcNow.AddMinutes(-1))
                return new ControlResult()
                {
                    CheckName = "AntiFlood",
                    Result = IControl.ControlResultType.skipped
                };
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.Value == "FloodControl")
                .SingleOrDefault();
            if (configValue != null)
                if (configValue.Value == "false")
                    return new ControlResult()
                    {
                        CheckName = "AntiFlood",
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
                        CheckName = "AntiFlood",
                        Result = IControl.ControlResultType.negative
                    };
                }

                currentValue.Messages += 1;
                currentValue.LastMessage = DateTime.UtcNow;
                FloodCounter[message.Chat.Id][message.From.Id] = currentValue;
            }

            if (FloodCounter[message.Chat.Id][message.From.Id].Messages >= 3)
            {
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
                                DateTime.UtcNow.AddMinutes(1)
                            ).Wait();

                Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    text: String.Format(
                        "User {0} muted due to flood in chat {1}.",
                        message.From.Id,
                        message.Chat.Title)
                );

                return new ControlResult()
                {
                    CheckName = "AntiFlood",
                    Result = IControl.ControlResultType.positive
                };
            }
            return new ControlResult()
            {
                CheckName = "AntiFlood",
                Result = IControl.ControlResultType.negative
            };
        }
    }
}
