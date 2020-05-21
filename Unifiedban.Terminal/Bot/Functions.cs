/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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
        static Filters.RTLNameFilter RTLNameFilter = new Filters.RTLNameFilter();

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
                reportChatId: CacheData.ControlChatId,
                rulesText: "No rules defined yet by the group admins. Just... be nice!",
                callerId: -2);
            if (registered == null)
                return false;

            CacheData.Groups.Add(message.Chat.Id, registered);
            CacheData.GroupConfigs.Add(message.Chat.Id, CacheData.GroupDefaultConfigs);
            if (MessageQueueManager.AddGroupIfNotPresent(registered))
            {
                Manager.BotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Your group {message.Chat.Title} has been added successfully!"
                );
                
                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "*[Report]*\n" +
                        "New group has chosen unified/ban ❗️\n" +
                        "\nChat: {0}" +
                        "\nChatId: {1}" +
                        "\n\n*hash_code:* UB{2}-{3}",
                        message.Chat.Title,
                        message.Chat.Id,
                        message.Chat.Id.ToString().Replace("-",""),
                        Guid.NewGuid())
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
                {
                    bool registered = RegisterGroup(message);
                    if (!registered)
                    {
                        Manager.BotClient.SendTextMessageAsync(
                            chatId: CacheData.ControlChatId,
                            parseMode: ParseMode.Markdown,
                            text: String.Format(
                                "*[Log]*\n" +
                                "⚠️Error registering new group.\n" +
                                "\nChat: {0}" +
                                "\nChatId: {1}" +
                                "\n\n*hash_code:* UB{2}-{3}",
                                message.Chat.Title,
                                message.Chat.Id,
                                message.Chat.Id.ToString().Replace("-",""),
                                Guid.NewGuid())
                        );
                    }
                }
            }

            if (!CacheData.Groups.ContainsKey(message.Chat.Id))
                return;

            bool blacklistEnabled = false;
            ConfigurationParameter blacklistConfig = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "Blacklist")
                .FirstOrDefault();
            if (blacklistConfig != null)
                if (blacklistConfig.Value.ToLower() == "true")
                    blacklistEnabled = true;

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

                Filters.FilterResult rtlCheck = RTLNameFilter.DoCheck(message,
                    member.FirstName + " " + member.LastName);
                if(rtlCheck.Result == Filters.IFilter.FilterResultType.positive)
                {
                    try
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
                            );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    continue;
                }

                if (blacklistEnabled)
                    if (CacheData.BannedUsers
                        .Where(x => x.TelegramUserId == member.Id).Count() > 0)
                    {
                        string author = member.Username == null
                            ? member.FirstName + " " + member.LastName
                            : member.Username;
                        
                        try
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
                                );
                            Manager.BotClient.KickChatMemberAsync(message.Chat.Id, member.Id);
                            
                            Manager.BotClient.SendTextMessageAsync(
                                chatId: CacheData.ControlChatId,
                                parseMode: ParseMode.Markdown,
                                text: String.Format(
                                    "*[Report]*\n" +
                                    "User in blacklist removed from chat.\n" +
                                    "\nUserId: {0}" +
                                    "\nUsername/Name: {1}" +
                                    "\nChat: {2}" +
                                    "\n\n*hash_code:* UB{3}-{4}",
                                    member.Id,
                                    author,
                                    message.Chat.Title,
                                    message.Chat.Id.ToString().Replace("-",""),
                                    Guid.NewGuid())
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Manager.BotClient.SendTextMessageAsync(
                                chatId: CacheData.ControlChatId,
                                parseMode: ParseMode.Markdown,
                                text: String.Format(
                                    "*[Log]*\n" +
                                    "⚠️Error removing blacklisted user from group.\n" +
                                    "\nUserId: {0}" +
                                    "\nUsername/Name: {1}" +
                                    "\nChat: {2}" +
                                    "\nChatId: {3}" +
                                    "\n\n*hash_code:* UB{4}-{5}",
                                    member.Id,
                                    author,
                                    message.Chat.Title,
                                    message.Chat.Id,
                                    message.Chat.Id.ToString().Replace("-",""),
                                    Guid.NewGuid())
                            );
                        }

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
                                ParseMode = ParseMode.Default,
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
                        BusinessLogic.ButtonLogic buttonLogic = new BusinessLogic.ButtonLogic();
                        List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();
                        foreach (Button btn in buttonLogic
                            .GetByChat(CacheData.Groups[message.Chat.Id]
                            .GroupId))
                        {
                            buttons.Add(new List<InlineKeyboardButton>());
                            buttons[buttons.Count -1].Add(InlineKeyboardButton.WithUrl(btn.Name, btn.Content));
                        }

                        MessageQueueManager.EnqueueMessage(
                           new ChatMessage()
                           {
                               Timestamp = DateTime.UtcNow,
                               Chat = message.Chat,
                               ParseMode = ParseMode.Html,
                               Text = Utils.Parsers.VariablesParser(
                                   CacheData.Groups[message.Chat.Id].WelcomeText,
                                   message, member),
                               ReplyMarkup = new InlineKeyboardMarkup(
                                        buttons
                                    )
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

        public static void CacheUsername(Message message)
        {
            if (message.From.Username != null)
                CacheData.Usernames[message.From.Username] = message.From.Id;
        }
    }
}
