/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using Unifiedban.Models;
using Unifiedban.Models.Group;
using System.Threading.Tasks;
using System.Timers;
using Telegram.Bot;
using Unifiedban.BusinessLogic.Group;
using Unifiedban.Terminal.Utils;
using Timer = System.Timers.Timer;

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

            lock (CacheData.GroupsLockObj)
            {
                CacheData.Groups.Add(message.Chat.Id, registered);
            }

            CacheData.GroupConfigs.Add(message.Chat.Id, CacheData.GroupDefaultConfigs);
            if (MessageQueueManager.AddGroupIfNotPresent(registered))
            {
                Manager.BotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    parseMode: ParseMode.Markdown,
                    text: CacheData.GetTranslation("en", "message_group_first_join"),
                    disableWebPagePreview: true
                );
                
                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "*[Log]*\n" +
                        "New group has chosen unified/ban 🥳\n" +
                        "\nChat: {0}" +
                        "\nChatId: {1}" +
                        "\n\n*hash_code:* #UB{2}-{3}",
                        message.Chat.Title,
                        message.Chat.Id,
                        message.Chat.Id.ToString().Replace("-",""),
                        Guid.NewGuid())
                );

                EnableDashboardForOwnerAndAdder(message.Chat);
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
                                "\n\n*hash_code:* #UB{2}-{3}",
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
                .FirstOrDefault(x => x.ConfigurationParameterId == "Blacklist");
            if (blacklistConfig != null)
                if (blacklistConfig.Value.ToLower() == "true")
                    blacklistEnabled = true;
            
            bool rtlNameCheckEnabled = false;
            ConfigurationParameter rtlNameCheckConfig = CacheData.GroupConfigs[message.Chat.Id]
                .FirstOrDefault(x => x.ConfigurationParameterId == "RTLNameFilter");
            if (rtlNameCheckConfig != null)
                if (rtlNameCheckConfig.Value.ToLower() == "true")
                    rtlNameCheckEnabled = true;

            bool captchaEnabled = false;
            ConfigurationParameter captchaConfig = CacheData.GroupConfigs[message.Chat.Id]
                .FirstOrDefault(x => x.ConfigurationParameterId == "Captcha");
            if (captchaConfig != null)
                if (captchaConfig.Value.ToLower() == "true")
                    captchaEnabled = true;

            bool welcomeMessageEnabled = false;
            ConfigurationParameter welcomeMessageConfig = CacheData.GroupConfigs[message.Chat.Id]
                .FirstOrDefault(x => x.ConfigurationParameterId == "WelcomeMessage");
            if (welcomeMessageConfig != null)
                if (welcomeMessageConfig.Value.ToLower() == "true")
                    welcomeMessageEnabled = true;

            if (!ChatTools.IsUserAdmin(message.Chat.Id, Manager.MyId))
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "added_to_group_not_admin")
                    });
                return;
            }

            if (!CacheData.ChatAdmins[message.Chat.Id][Manager.MyId].CanRestrictMembers)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "added_to_group_not_adminPrivilege")
                    });
                return;
            }

            foreach (User member in message.NewChatMembers)
            {
                if (member.Id == Manager.MyId ||
                    member.Id == 777000 || // Telegram's official updateServiceNotification
                    member.IsBot) 
                {
                    continue;
                }

                if (blacklistEnabled)
                {
                    if (Utils.UserTools.KickIfIsInBlacklist(message, member))
                    {
                        continue;
                    }
                }

                if (rtlNameCheckEnabled)
                {
                    Filters.FilterResult rtlCheck = RTLNameFilter.DoCheck(message,
                        member.FirstName + " " + member.LastName);
                    if (rtlCheck.Result == Filters.IFilter.FilterResultType.positive)
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
                            Data.Utils.Logging.AddLog(new SystemLog()
                            {
                                LoggerName = CacheData.LoggerName,
                                Date = DateTime.Now,
                                Function = "UserJoinedAction -> rtlNameCheckEnabled",
                                Level = SystemLog.Levels.Error,
                                Message = ex.Message,
                                UserId = -1
                            });

                            if (ex.InnerException != null)
                            {
                                Data.Utils.Logging.AddLog(new SystemLog()
                                {
                                    LoggerName = CacheData.LoggerName,
                                    Date = DateTime.Now,
                                    Function = "UserJoinedAction -> rtlNameCheckEnabled",
                                    Level = SystemLog.Levels.Error,
                                    Message = ex.Message,
                                    UserId = -1
                                });
                            }
                        }

                        continue;
                    }
                }

                try
                {
                    bool pluginCheckOk = true;
                    foreach (var plugin in CacheData.PreCaptchaAndWelcomePlugins)
                    {
                        if (!plugin.Execute(message, member, MessageQueueManager.EnqueueMessage))
                        {
                            pluginCheckOk = false;
                            try
                            {
                                Thread.Sleep(300);
                                Manager.BotClient.KickChatMemberAsync(message.Chat.Id, member.Id);
                                if (message.Chat.Type == ChatType.Supergroup)
                                {
                                    Thread.Sleep(500);
                                    Manager.BotClient.UnbanChatMemberAsync(message.Chat.Id, member.Id);
                                }

                                break;
                            }
                            catch (Exception ex)
                            {
                                Data.Utils.Logging.AddLog(new Models.SystemLog()
                                {
                                    LoggerName = CacheData.LoggerName,
                                    Date = DateTime.Now,
                                    Function = "Member joined",
                                    Level = SystemLog.Levels.Error,
                                    Message = "Error kicking user for failed pre-join check",
                                    UserId = -2
                                });
                                Data.Utils.Logging.AddLog(new Models.SystemLog()
                                {
                                    LoggerName = CacheData.LoggerName,
                                    Date = DateTime.Now,
                                    Function = "Member joined",
                                    Level = SystemLog.Levels.Error,
                                    Message = ex.Message,
                                    UserId = -2
                                });

                                if (ex.InnerException != null)
                                {

                                    Data.Utils.Logging.AddLog(new Models.SystemLog()
                                    {
                                        LoggerName = CacheData.LoggerName,
                                        Date = DateTime.Now,
                                        Function = "Member joined",
                                        Level = SystemLog.Levels.Error,
                                        Message = ex.InnerException.Message,
                                        UserId = -2
                                    });
                                }

                                break;
                            }
                        }
                    }

                    if (!pluginCheckOk)
                    {
                        continue;
                    }
                    
                    if (CacheData.TrustFactors.ContainsKey(member.Id))
                    {
                        int points = CacheData.TrustFactors[member.Id].Points;
                        if (points < 71)
                        {
                            Manager.BotClient.SendTextMessageAsync(
                                chatId: message.Chat,
                                parseMode: ParseMode.Markdown,
                                text: String.Format(
                                    "*[Alert]*\n" +
                                    "⚠️ Trust factor of user {0}:{1} is below security threshold\n" +
                                    "\n*Trust factor:* {2}/100",
                                    member.Id,
                                    member.Username,
                                    points)
                            );
                        }
                    }
                    if (captchaEnabled && !BotTools.IsUserOperator(member.Id))
                    {
                        Manager.BotClient.RestrictChatMemberAsync(
                                message.Chat.Id,
                                member.Id,
                                new ChatPermissions()
                                {
                                    CanSendMessages = false,
                                    CanChangeInfo = false,
                                    CanInviteUsers = false,
                                    CanPinMessages = false,
                                    CanSendMediaMessages = false,
                                    CanSendOtherMessages = false,
                                    CanSendPolls = false
                                }
                            ).Wait();
                        
                        string timerKey = $"{message.Chat.Id}:{member.Id}";

                        string name = member.Username != null ? "@" + member.Username : member.FirstName;
                        int imgSent = ChatTools.SendCaptchaImage(message.Chat, name, member.Id, timerKey);

                        if (imgSent == -1)
                        {
                            MessageQueueManager.EnqueueMessage(
                                new Models.ChatMessage()
                                {
                                    Timestamp = DateTime.UtcNow,
                                    Chat = message.Chat,
                                    Text =
                                        $"Please {name} certify to be a human.\nIf you don't click this button you are not going to be unlocked.",
                                    ReplyMarkup = new InlineKeyboardMarkup(
                                        InlineKeyboardButton.WithCallbackData(
                                            CacheData.GetTranslation("en", "captcha_iamhuman", true),
                                            $"/Captcha " + member.Id + " " + timerKey
                                        )
                                    ),
                                    PostSentAction = ChatMessage.PostSentActions.Destroy,
                                    AutoDestroyTimeInSeconds = (ushort)(60 * CacheData.CaptchaAutoKickTimer)
                                });
                        }
                        
                        var timer = new Timer()
                        {
                            Interval = 1000 * 60 * CacheData.CaptchaAutoKickTimer,
                            AutoReset = false
                        };
                        timer.Elapsed += delegate(object sender, ElapsedEventArgs args)
                        {
                            Manager.BotClient.KickChatMemberAsync(message.Chat, member.Id);
                            if (message.Chat.Type == ChatType.Supergroup)
                            {
                                Thread.Sleep(500);
                                Manager.BotClient.UnbanChatMemberAsync(message.Chat.Id, member.Id);
                            }

                            if (imgSent != -1)
                            {
                                Manager.BotClient.DeleteMessageAsync(message.Chat.Id, imgSent);
                            }
                        };

                        if (CacheData.CaptchaAutoKickTimers.ContainsKey(timerKey))
                        {
                            if (!CacheData.CaptchaAutoKickTimers[timerKey].Enabled)
                            {
                                CacheData.CaptchaAutoKickTimers.TryRemove(timerKey, out var oldTimer);
                            }
                        }
                        
                        if (CacheData.CaptchaAutoKickTimers.TryAdd(timerKey, timer))
                        {
                            CacheData.CaptchaAutoKickTimers[timerKey].Start();
                        }
                        else
                        {
                            Data.Utils.Logging.AddLog(new Models.SystemLog()
                            {
                                LoggerName = CacheData.LoggerName,
                                Date = DateTime.Now,
                                Function = "Unifiedban Functions - User Join",
                                Level = SystemLog.Levels.Warn,
                                Message = "Error creating timer for auto captcha auto kick",
                                UserId = -2
                            });
                        }

                        continue;
                    }

                    if (welcomeMessageEnabled)
                    {
                        BusinessLogic.ButtonLogic buttonLogic = new BusinessLogic.ButtonLogic();
                        List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();
                        int btnCount = 0;
                        int depthLevel = 0;
                        buttons.Add(new List<InlineKeyboardButton>());
                        
                        foreach (Button btn in buttonLogic
                            .GetByChat(CacheData.Groups[message.Chat.Id]
                            .GroupId))
                        {
                            if (btnCount == 2)
                            {
                                btnCount = 0;
                                buttons.Add(new List<InlineKeyboardButton>());
                                depthLevel++;
                            }
                            buttons[depthLevel].Add(InlineKeyboardButton.WithUrl(btn.Name, btn.Content));
                            btnCount++;
                        }

                        MessageQueueManager.EnqueueMessage(
                           new Models.ChatMessage()
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
                    
                    foreach (var plugin in CacheData.PostCaptchaAndWelcomePlugins)
                    {
                        if (!plugin.Execute(message, member, MessageQueueManager.EnqueueMessage))
                        {
                            Manager.BotClient.KickChatMemberAsync(message.Chat.Id, member.Id);
                            if (message.Chat.Type == ChatType.Supergroup)
                            {
                                Thread.Sleep(500);
                                Manager.BotClient.UnbanChatMemberAsync(message.Chat.Id, member.Id);
                            }

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Data.Utils.Logging.AddLog(new SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "UserJoinedAction",
                        Level = SystemLog.Levels.Error,
                        Message = ex.Message,
                        UserId = -1
                    });

                    if(ex.InnerException != null)
                    {
                        Data.Utils.Logging.AddLog(new SystemLog()
                        {
                            LoggerName = CacheData.LoggerName,
                            Date = DateTime.Now,
                            Function = "UserJoinedAction",
                            Level = SystemLog.Levels.Error,
                            Message = ex.Message,
                            UserId = -1
                        });
                    }
                }
            }
        }

        public static void MigrateToChatId(Message message)
        {
            if (!CacheData.Groups.ContainsKey(message.Chat.Id))
                return;

            var telegramGroupLogic =
               new BusinessLogic.Group.TelegramGroupLogic();

            var updated = telegramGroupLogic.UpdateChatId(message.Chat.Id, (long)message.MigrateToChatId, -2); // TODO - log operation
            if (updated == null)
                return;
            lock (CacheData.GroupsLockObj)
            {
                CacheData.Groups.Add(updated.TelegramChatId, updated);
            }

            MessageQueueManager.AddGroupIfNotPresent(updated);
            
            //MessageQueueManager.RemoveGroupIfNotPresent(message.Chat.Id);
            //CacheData.Groups.Remove(message.Chat.Id);
        }

        public static void CacheUsername(Message message)
        {
            if (message.From.Username != null)
                CacheData.Usernames[message.From.Username] = message.From.Id;
        }

        public static void UserLeftAction(Message message)
        {
            if (message.LeftChatMember == null) return;
            ChatTools.RemoveChatAdmin(message.Chat.Id, message.LeftChatMember.Id);
            if (!BotTools.IsUserOperator(message.LeftChatMember.Id)) return;
            if (!CacheData.ActiveSupport
                .Contains(message.Chat.Id)) return;
            
            if (CacheData.CurrentChatOperators.ContainsKey(message.Chat.Id))
            {
                CacheData.CurrentChatOperators[message.Chat.Id].Remove(message.LeftChatMember.Id);
                if (CacheData.CurrentChatOperators[message.Chat.Id].Count > 0) return;
            }
            
            CacheData.CurrentChatOperators.Remove(message.Chat.Id);
            CacheData.ActiveSupport.Remove(message.Chat.Id);

            Manager.BotClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                parseMode: ParseMode.Markdown,
                text: $"Support session ended since " +
                      $"operator *{message.LeftChatMember.Username}* left the chat."
            );
            MessageQueueManager.EnqueueLog(new ChatMessage()
            {
                ParseMode = ParseMode.Markdown,
                Text = string.Format(
                    "*[Log]*" +
                    "Support session ended since operator *{0}* left the chat." +
                    "\nChatId: `{1}`" +
                    "\nChat: `{2}`" +
                    "\nUserId: `{3}`" +
                    "\n\n*hash_code:* #UB{4}-{5}",
                    message.LeftChatMember.Username,
                    message.Chat.Id,
                    message.Chat.Title,
                    message.LeftChatMember.Id,
                    message.Chat.Id.ToString().Replace("-", ""),
                    Guid.NewGuid())
            });
        }

        private static void EnableDashboardForOwnerAndAdder(Chat chat)
        {
            var admins = Manager.BotClient.GetChatAdministratorsAsync(chat!).Result;
            var creator = admins.FirstOrDefault(x => x.Status == ChatMemberStatus.Creator);
            if (creator is null) return;
            
            DashboardUserLogic dul = new DashboardUserLogic();
            DashboardUser user = dul.GetByTelegramUserId(creator.User.Id);
            
            if (user == null)
            {
                string profilePic = "";
                var photos = Manager.BotClient.GetUserProfilePhotosAsync(creator.User.Id).Result;
                if (photos.TotalCount > 0)
                {
                    profilePic = photos.Photos[0][0].FileId;
                }

                user = dul.Add(creator.User.Id,
                    creator.User.FirstName + " " + creator.User.LastName,
                    profilePic, -2);
                if (user == null)
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = chat,
                            ParseMode = ParseMode.Markdown,
                            Text = "*[Report]*\n" +
                                   "Error enabling dashboard!"
                        });
                    return;
                }
            }
            
            DashboardPermissionLogic dpl = new DashboardPermissionLogic();
            DashboardPermission permission =  dpl.GetByUserId(user.DashboardUserId,
                CacheData.Groups[chat.Id].GroupId);

            if (permission == null)
            {
                if (dpl.Add(CacheData.Groups[chat.Id].GroupId,
                    user.DashboardUserId, DashboardPermission.Status.Active,
                    -2) == null)
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = chat,
                            ParseMode = ParseMode.Markdown,
                            Text = "*[Report]*\n" +
                                   "Error enabling dashboard!"
                        });
                }
            }
        }
    }
}
