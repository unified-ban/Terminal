/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Unifiedban.Models;
using Unifiedban.Terminal.Bot;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Controls
{
    public class Manager
    {
        static List<IControl> controls = new List<IControl>();
        static List<Filters.IFilter> filters = new List<Filters.IFilter>();

        public static void Initialize()
        {
            controls.Add(new SpamNameControl());
            controls.Add(new FloodControl());
            controls.Add(new SafeGroupControl());
            controls.Add(new Notes());

            filters.Add(new Filters.BadWordFilter());
            filters.Add(new Filters.NonLatinFilter());
            filters.Add(new Filters.ScamFilter());
            filters.Add(new Filters.RTLNameFilter());

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Controls and filters initialized",
                UserId = -2
            });
        }

        public static void DoCheck(Message message)
        {
            foreach (var plugin in CacheData.PreControlsPlugins)
            {
                if (!plugin.Execute(message, MessageQueueManager.EnqueueMessage, Bot.Manager.BotClient))
                {
                    return;
                }
            }
            
            foreach (IControl control in controls)
            {
                ControlResult result = control.DoCheck(message);
                if (result.Result == IControl.ControlResultType.positive)
                {
                    var actionToTake = CacheData.GroupConfigs[message.Chat.Id]
                        .SingleOrDefault(x => x.ConfigurationParameterId == "SpamAction");
                    if (actionToTake == null)
                    {
                        RemoveMessageForPositiveControl(message, result);
                        return;
                    }

                    switch (actionToTake.Value)
                    {
                        case "delete":
                            RemoveMessageForPositiveControl(message, result);
                            break;
                        case "limit":
                            LimitUserForPositiveControl(message, result);
                            break;
                        case "ban":
                            BanUserForPositiveControl(message, result);
                            break;
                    }

                    return;
                }
            }
            
            foreach (var plugin in CacheData.PostControlsPlugins)
            {
                if (!plugin.Execute(message, MessageQueueManager.EnqueueMessage, Bot.Manager.BotClient))
                {
                    return;
                }
            }
            
            foreach (var plugin in CacheData.PreFiltersPlugins)
            {
                if (!plugin.Execute(message, MessageQueueManager.EnqueueMessage, Bot.Manager.BotClient))
                {
                    return;
                }
            }

            foreach (Filters.IFilter filter in filters)
            {
                Filters.FilterResult result = filter.DoCheck(message);
                if(result.Result == Filters.IFilter.FilterResultType.positive)
                {
                    var actionToTake = CacheData.GroupConfigs[message.Chat.Id]
                        .SingleOrDefault(x => x.ConfigurationParameterId == "SpamAction");
                    if (actionToTake == null)
                    {
                        RemoveMessageForPositiveFilter(message, result);
                        return;
                    }

                    switch (actionToTake.Value)
                    {
                        case "delete":
                            RemoveMessageForPositiveFilter(message, result);
                            break;
                        case "limit":
                            LimitUserForPositiveFilter(message, result);
                            break;
                        case "ban":
                            BanUserForPositiveFilter(message, result);
                            break;
                    }
                    return;
                }
            }
            
            foreach (var plugin in CacheData.PostFiltersPlugins)
            {
                if (!plugin.Execute(message, MessageQueueManager.EnqueueMessage, Bot.Manager.BotClient))
                {
                    return;
                }
            }
        }

        public static void DoMediaCheck(Message message)
        {
            // TODO !
        }

        private static void RemoveMessageForPositiveControl(Message message, ControlResult result)
        {
            try
            {
                Bot.Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                string author = message.From.Username == null
                    ? message.From.FirstName + " " + message.From.LastName
                    : "@" + message.From.Username;
                string logMessage = String.Format(
                    "*[Report]*\n" +
                    "Message deleted due to control *{0}*.\n" +
                    "⚠ do not open links you don't know ⚠\n" +
                    "\nChat: `{1}`" +
                    "\nAuthor: `{3}`" +
                    "\nUserId: `{4}`" +
                    "\nOriginal message:\n```{2}```" +
                    "\n\n*hash_code:* #UB{5}-{6}",
                    result.CheckName,
                    message.Chat.Title,
                    message.Text,
                    author,
                    message.From.Id,
                    message.Chat.Id.ToString().Replace("-", ""),
                    Guid.NewGuid());
                MessageQueueManager.EnqueueLog(new ChatMessage()
                {
                    ParseMode = ParseMode.Markdown,
                    Text = logMessage
                });
                
                LogTools.AddActionLog(new ActionLog()
                {
                    GroupId = CacheData.Groups[message.Chat.Id].GroupId,
                    UtcDate = DateTime.UtcNow,
                    ActionTypeId = "autoDelete",
                    Parameters = logMessage,
                });
            }
            catch (Exception ex)
            {
                Data.Utils.Logging.AddLog(new SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Terminal.Controls.Manager.RemoveMessageForPositiveControl",
                    Level = SystemLog.Levels.Error,
                    Message = ex.Message,
                    UserId = -1
                });
                if(ex.InnerException != null)
                    Data.Utils.Logging.AddLog(new SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Unifiedban.Terminal.Controls.Manager.RemoveMessageForPositiveControl",
                        Level = SystemLog.Levels.Error,
                        Message = ex.InnerException.Message,
                        UserId = -1
                    });
            }
        }

        private static void RemoveMessageForPositiveFilter(Message message, Filters.FilterResult result)
        {
            try
            {
                Bot.Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                string author = message.From.Username == null
                    ? message.From.FirstName + " " + message.From.LastName
                    : "@" + message.From.Username;
                string logMessage = String.Format(
                    "*[Report]*\n" +
                    "Message deleted due to filter *{0}* provided positive result.\n" +
                    "⚠ do not open links you don't know ⚠\n" +
                    "\nChat: `{1}`" +
                    "\nAuthor: `{3}`" +
                    "\nUserId: `{4}`" +
                    "\nOriginal message:\n```{2}```" +
                    "\n\n*hash_code:* #UB{5}-{6}",
                    result.CheckName,
                    message.Chat.Title,
                    message.Text,
                    author,
                    message.From.Id,
                    message.Chat.Id.ToString().Replace("-", ""),
                    Guid.NewGuid());
                MessageQueueManager.EnqueueLog(new ChatMessage()
                {
                    ParseMode = ParseMode.Markdown,
                    Text = logMessage
                });
                
                LogTools.AddActionLog(new ActionLog()
                {
                    GroupId = CacheData.Groups[message.Chat.Id].GroupId,
                    UtcDate = DateTime.UtcNow,
                    ActionTypeId = "autoDelete",
                    Parameters = logMessage,
                });
            }
            catch (Exception ex)
            {
                Data.Utils.Logging.AddLog(new SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Terminal.Controls.Manager.RemoveMessageForPositiveFilter",
                    Level = SystemLog.Levels.Error,
                    Message = ex.Message,
                    UserId = -1
                });
                if(ex.InnerException != null)
                    Data.Utils.Logging.AddLog(new SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Unifiedban.Terminal.Controls.Manager.RemoveMessageForPositiveFilter",
                        Level = SystemLog.Levels.Error,
                        Message = ex.InnerException.Message,
                        UserId = -1
                    });
            }
        }

        private static void LimitUserForPositiveControl(Message message, ControlResult result)
        {
            int limitTime = 3;
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "SpamActionLimitTime")
                .SingleOrDefault();
            if (configValue != null)
            {
                int.TryParse(configValue.Value, out limitTime);
            }
            RemoveMessageForPositiveControl(message, result);
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
                DateTime.UtcNow.AddMinutes(limitTime));
            
            string author = message.From.Username == null
                ? message.From.FirstName + " " + message.From.LastName
                : "@" + message.From.Username;
            string logMessage = String.Format(
                "*[Report]*\n" +
                "User limited as per group _Spam Action_ preference.\n" +
                "⚠ do not open links you don't know ⚠\n" +
                "\nControl: `{0}`" +
                "\nChat: `{1}`" +
                "\nAuthor: `{2}`" +
                "\nUserId: `{3}`" +
                "\n\n*hash_code:* #UB{4}-{5}",
                result.CheckName,
                message.Chat.Title,
                author,
                message.From.Id,
                message.Chat.Id.ToString().Replace("-", ""),
                Guid.NewGuid());
            MessageQueueManager.EnqueueLog(new ChatMessage()
            {
                ParseMode = ParseMode.Markdown,
                Text = logMessage
            });
                
            LogTools.AddActionLog(new ActionLog()
            {
                GroupId = CacheData.Groups[message.Chat.Id].GroupId,
                UtcDate = DateTime.UtcNow,
                ActionTypeId = "autoLimit",
                Parameters = logMessage,
            });
        }
        private static void LimitUserForPositiveFilter(Message message, Filters.FilterResult result)
        {
            
            int limitTime = 3;
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "SpamActionLimitTime")
                .SingleOrDefault();
            if (configValue != null)
            {
                int.TryParse(configValue.Value, out limitTime);
            }
            RemoveMessageForPositiveFilter(message, result);
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
                DateTime.UtcNow.AddMinutes(limitTime));
            
            string author = message.From.Username == null
                ? message.From.FirstName + " " + message.From.LastName
                : "@" + message.From.Username;
            string logMessage = String.Format(
                "*[Report]*\n" +
                "User limited as per group _Spam Action_ preference.\n" +
                "⚠ do not open links you don't know ⚠\n" +
                "\nControl: `{0}`" +
                "\nChat: `{1}`" +
                "\nAuthor: `{2}`" +
                "\nUserId: `{3}`" +
                "\n\n*hash_code:* #UB{4}-{5}",
                result.CheckName,
                message.Chat.Title,
                author,
                message.From.Id,
                message.Chat.Id.ToString().Replace("-", ""),
                Guid.NewGuid());
            MessageQueueManager.EnqueueLog(new ChatMessage()
            {
                ParseMode = ParseMode.Markdown,
                Text = logMessage
            });
                
            LogTools.AddActionLog(new ActionLog()
            {
                GroupId = CacheData.Groups[message.Chat.Id].GroupId,
                UtcDate = DateTime.UtcNow,
                ActionTypeId = "autoLimit",
                Parameters = logMessage,
            });
        }
        private static void BanUserForPositiveControl(Message message, ControlResult result)
        {
            
            int limitTime = 3;
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "SpamActionLimitTime")
                .SingleOrDefault();
            if (configValue != null)
            {
                int.TryParse(configValue.Value, out limitTime);
            }
            RemoveMessageForPositiveControl(message, result);
            
            Bot.Manager.BotClient.KickChatMemberAsync(message.Chat.Id, message.From.Id,
                DateTime.UtcNow.AddMinutes(-5));
            
            UserTools.AddPenalty(message.Chat.Id, message.From.Id,
                Models.TrustFactorLog.TrustFactorAction.ban, Bot.Manager.MyId);
            
            string author = message.From.Username == null
                ? message.From.FirstName + " " + message.From.LastName
                : "@" + message.From.Username;
            string logMessage = String.Format(
                "*[Report]*\n" +
                "User banned as per group _Spam Action_ preference.\n" +
                "⚠ do not open links you don't know ⚠\n" +
                "\nControl: `{0}`" +
                "\nChat: `{1}`" +
                "\nAuthor: `{2}`" +
                "\nUserId: `{3}`" +
                "\n\n*hash_code:* #UB{4}-{5}",
                result.CheckName,
                message.Chat.Title,
                author,
                message.From.Id,
                message.Chat.Id.ToString().Replace("-", ""),
                Guid.NewGuid());
            MessageQueueManager.EnqueueLog(new ChatMessage()
            {
                ParseMode = ParseMode.Markdown,
                Text = logMessage
            });
                
            LogTools.AddActionLog(new ActionLog()
            {
                GroupId = CacheData.Groups[message.Chat.Id].GroupId,
                UtcDate = DateTime.UtcNow,
                ActionTypeId = "autoBan",
                Parameters = logMessage,
            });
        }
        private static void BanUserForPositiveFilter(Message message, Filters.FilterResult result)
        {
            
            int limitTime = 3;
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "SpamActionLimitTime")
                .SingleOrDefault();
            if (configValue != null)
            {
                int.TryParse(configValue.Value, out limitTime);
            }
            RemoveMessageForPositiveFilter(message, result);
            
            Bot.Manager.BotClient.KickChatMemberAsync(message.Chat.Id, message.From.Id,
                DateTime.UtcNow.AddMinutes(-5));
            UserTools.AddPenalty(message.Chat.Id, message.From.Id,
                Models.TrustFactorLog.TrustFactorAction.ban, Bot.Manager.MyId);
            
            Bot.Manager.BotClient.KickChatMemberAsync(message.Chat.Id, message.From.Id,
                DateTime.UtcNow.AddMinutes(-5));
            
            UserTools.AddPenalty(message.Chat.Id, message.From.Id,
                Models.TrustFactorLog.TrustFactorAction.ban, Bot.Manager.MyId);
            
            string author = message.From.Username == null
                ? message.From.FirstName + " " + message.From.LastName
                : "@" + message.From.Username;
            string logMessage = String.Format(
                "*[Report]*\n" +
                "User banned as per group _Spam Action_ preference.\n" +
                "⚠ do not open links you don't know ⚠\n" +
                "\nControl: `{0}`" +
                "\nChat: `{1}`" +
                "\nAuthor: `{2}`" +
                "\nUserId: `{3}`" +
                "\n\n*hash_code:* #UB{4}-{5}",
                result.CheckName,
                message.Chat.Title,
                author,
                message.From.Id,
                message.Chat.Id.ToString().Replace("-", ""),
                Guid.NewGuid());
            MessageQueueManager.EnqueueLog(new ChatMessage()
            {
                ParseMode = ParseMode.Markdown,
                Text = logMessage
            });
                
            LogTools.AddActionLog(new ActionLog()
            {
                GroupId = CacheData.Groups[message.Chat.Id].GroupId,
                UtcDate = DateTime.UtcNow,
                ActionTypeId = "autoBan",
                Parameters = logMessage,
            });
        }

        public static bool IsTelegramLink(string siteUri)
        {
            using WebClientWithTimeout client = new WebClientWithTimeout();
            client.Headers.Add("Accept", " text/html, application/xhtml+xml, */*");
            client.Headers.Add("Content-Type", "application/json;charset=UTF-8");
            client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.66");
                
            var htmlCode = "";
            try
            {
                htmlCode = client.DownloadString(siteUri);
                    
                if (htmlCode.Contains("tgme_page_extra") && htmlCode.Contains("member"))
                    return true;
            }
            catch (Exception ex)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Terminal.Controls.Manager.IsTelegramLink",
                    Level = Models.SystemLog.Levels.Debug,
                    Message = $"Error loading siteUri: {siteUri}\n{ex.Message}",
                    UserId = -2
                });
            }
                
            return false;
        }
    }

    public class WebClientWithTimeout : System.Net.WebClient
    {
        protected override System.Net.WebRequest GetWebRequest(Uri address)
        {
            var wr = base.GetWebRequest(address);
            wr.Timeout = 5000; // timeout in milliseconds (ms)
            return wr;
        }
    }
}
