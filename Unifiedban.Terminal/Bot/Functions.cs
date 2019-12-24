using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using Unifiedban.Models;
using Unifiedban.Models.Group;
using System.Threading.Tasks;

namespace Unifiedban.Terminal.Bot
{
    public class Functions
    {
        public static bool RegisterGroup(Message message)
        {
            if (CacheData.Groups.ContainsKey(message.Chat.Id))
                return false;

            BusinessLogic.Group.TelegramGroupLogic telegramGroupLogic =
                new BusinessLogic.Group.TelegramGroupLogic();
            TelegramGroup registered = telegramGroupLogic.Add(
                message.Chat.Id, message.Chat.Title, TelegramGroup.Status.Active,
                configuration: Newtonsoft.Json.JsonConvert.SerializeObject(CacheData.GroupDefaultConfigs),
                welcomeText: CacheData.GetTranslation("en", "message_welcome_default"),
                chatLanguage: "en",
                settingsLanguage: "en",
                reportChatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                rulesText: "No rules defined yet by the group admins. Just... be nice!",
                callerId: -2);
            if (registered == null)
                return false;

            CacheData.Groups.Add(message.Chat.Id, registered);
            if (MessageQueueManager.AddGroupIfNotPresent(registered))
            {
                Manager.BotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Your group {message.Chat.Title} has been added successfully!"
                );
            }

            return true;
        }

        public static void UserJoinedAction(Message message)
        {
            if(message.NewChatMembers.SingleOrDefault(x => x.Id == Manager.MyId) != null &&
                        (message.Chat.Type == ChatType.Group ||
                         message.Chat.Type == ChatType.Supergroup))
            {
                Data.Utils.Logging.AddLog(new SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Bot.Manager.BotClient_OnMessage",
                    Level = SystemLog.Levels.Info,
                    Message = $"I have been added to Group {message.Chat.Title} ID {message.Chat.Id}",
                    UserId = -1
                });

                if (!CacheData.Groups.ContainsKey(message.Chat.Id))
                    Data.Utils.Logging.AddLog(new SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Unifiedban.Bot.Manager.BotClient_OnMessage",
                        Level = SystemLog.Levels.Info,
                        Message = $"Group {message.Chat.Title} ID {message.Chat.Id} is already registered",
                        UserId = -1
                    });

                bool registered = RegisterGroup(message);
                if (!registered)
                {
                    Manager.BotClient.SendTextMessageAsync(
                        chatId: Convert.ToInt64(CacheData.SysConfigs
                                    .Single(x => x.SysConfigId == "ControlChatId")
                                    .Value),
                        parseMode: ParseMode.Markdown,
                        text: $"Error registering the group with chat Id {message.Chat.Id}"
                    );
                }
            }

            if (!CacheData.Groups.ContainsKey(message.Chat.Id))
                return;

            bool captchaEnabled = false;
            ConfigurationParameter captchaConfig = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "Captcha")
                .FirstOrDefault();
            if (captchaConfig != null)
                if (captchaConfig.Value.ToLower() == "true")
                    captchaEnabled = true;

            bool welcomeMessageEnabled = false;
            ConfigurationParameter welcomeMessageConfig = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "WelcomeMessage")
                .FirstOrDefault();
            if (welcomeMessageConfig != null)
                if (welcomeMessageConfig.Value.ToLower() == "true")
                    welcomeMessageEnabled = true;

            foreach (User member in message.NewChatMembers)
            {
                if (member.Id == Manager.MyId)
                {
                    continue;
                }

                try
                {
                    if (captchaEnabled && CacheData.Operators
                        .SingleOrDefault(x => x.TelegramUserId == member.Id) == null)
                    {
                        Manager.BotClient.RestrictChatMemberAsync(
                                message.Chat.Id,
                                member.Id,
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
                                }
                            ).Wait();

                        string name = member.Username != null ? "@" + member.Username : member.FirstName;
                        MessageQueueManager.EnqueueMessage(
                            new ChatMessage()
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = message.Chat,
                                ParseMode = ParseMode.Markdown,
                                Text = $"Please {name} certify to be a human.\nIf you don't click this button you are not going to be unlocked.",
                                ReplyMarkup = new InlineKeyboardMarkup(
                                    InlineKeyboardButton.WithCallbackData(
                                        CacheData.GetTranslation("en", "captcha_iamhuman", true),
                                        $"/Captcha " + member.Id
                                        )
                                )
                            });

                        continue;
                    }

                    if (welcomeMessageEnabled)
                    {
                        MessageQueueManager.EnqueueMessage(
                           new ChatMessage()
                           {
                               Timestamp = DateTime.UtcNow,
                               Chat = message.Chat,
                               ParseMode = ParseMode.Html,
                               Text = Utils.Parsers.VariablesParser(
                                   CacheData.Groups[message.Chat.Id].WelcomeText,
                                   message),
                           });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static void MigrateToChatId(Message message)
        {
            if (!CacheData.Groups.ContainsKey(message.Chat.Id))
                return;

            BusinessLogic.Group.TelegramGroupLogic telegramGroupLogic =
               new BusinessLogic.Group.TelegramGroupLogic();

            TelegramGroup updated = telegramGroupLogic.UpdateChatId(message.Chat.Id, message.MigrateToChatId, -2); // TODO - log operation
            if (updated == null)
                return;

            CacheData.Groups[message.Chat.Id].TelegramChatId = message.MigrateToChatId;
            CacheData.Groups.Add(message.MigrateToChatId, CacheData.Groups[message.Chat.Id]);
            CacheData.Groups.Remove(message.Chat.Id);
            MessageQueueManager.AddGroupIfNotPresent(updated);
            MessageQueueManager.RemoveGroupIfNotPresent(message.Chat.Id);
        }
    }
}
