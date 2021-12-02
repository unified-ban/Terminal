/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Unifiedban.BusinessLogic.Group;
using Unifiedban.Models.Group;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Config : ICommand
    {
        public void Execute(Message message)
        {
            if (message.Chat.Type == ChatType.Private ||
                message.Chat.Type == ChatType.Channel)
            {
                return;
            }
            
            Execute(message, false);
        }

        public void Execute(Message message, bool isUpdate = false)
        {
            bool isOperator = Utils.BotTools.IsUserOperator(message.From.Id);
            if (!isOperator &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return;
            }

            if (!isUpdate)
            {
                var me = Manager.BotClient.GetChatMemberAsync(message.Chat.Id, Manager.MyId).Result;
                if (me is ChatMemberAdministrator { CanDeleteMessages: true })
                    Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }

            string settingsLang = CacheData.Groups[message.Chat.Id].SettingsLanguage;

            List<List<InlineKeyboardButton>> configMenu = new List<List<InlineKeyboardButton>>();
            int btnCount;
            int depthLevel = 0;

            configMenu.Add(new List<InlineKeyboardButton>());
            configMenu[0].Add(InlineKeyboardButton.WithUrl("Instructions", "https://docs.unifiedban.solutions/docs/setup"));
            configMenu[0].Add(InlineKeyboardButton.WithUrl("Dashboard", "https://dash.unifiedban.solutions/"));
            btnCount = 2;

            try
            {
                foreach (ConfigurationParameter conf in CacheData.GroupConfigs[message.Chat.Id])
                {
                    if (btnCount == 2)
                    {
                        btnCount = 0;
                        configMenu.Add(new List<InlineKeyboardButton>());
                        depthLevel++;
                    }

                    string icon = conf.Value == "true" ? " ✅" : " ❌";
                    string newSet = conf.Value == "true" ? "false" : "true";

                    switch (conf.Type)
                    {
                        case "bool":
                            configMenu[depthLevel].Add(InlineKeyboardButton.WithCallbackData(
                                CacheData.GetTranslation(settingsLang, conf.ConfigurationParameterId, true) + " " + icon,
                                $"/config { message.From.Id } { conf.ConfigurationParameterId }|{ newSet }"
                                ));
                            break;
                        case "multiselect":
                        case "int":
                        case "string":
                        case "language":
                            configMenu[depthLevel].Add(InlineKeyboardButton.WithCallbackData(
                                CacheData.GetTranslation(settingsLang, conf.ConfigurationParameterId, true),
                                $"/config { message.From.Id } getValue|{ conf.ConfigurationParameterId }"
                                ));
                            break;
                        case "boolfunction":
                            configMenu[depthLevel].Add(InlineKeyboardButton.WithCallbackData(
                                CacheData.GetTranslation(settingsLang, conf.ConfigurationParameterId, true) + " " + icon,
                                $"/config { message.From.Id } exec|{ conf.ConfigurationParameterId }|{ newSet }"
                                ));
                            break;
                    }

                    btnCount++;
                }
            }
            catch
            {

            }

            configMenu.Add(new List<InlineKeyboardButton>());
            configMenu[depthLevel + 1].Add(InlineKeyboardButton.WithUrl("Ask for support", "https://t.me/unifiedban_group"));
            configMenu[depthLevel + 1].Add(InlineKeyboardButton.WithUrl("Docs", "https://docs.unifiedban.solutions/docs/"));

            configMenu.Add(new List<InlineKeyboardButton>());
            bool dashboardStatus = getDashboardStatus(message);
            if (dashboardStatus || isOperator)
            {
                configMenu[depthLevel + 2].Add(InlineKeyboardButton.WithUrl("Dashboard", "https://dash.unifiedban.solutions/"));
            }
            else
            {
                configMenu[depthLevel + 2].Add(InlineKeyboardButton.WithCallbackData(
                    CacheData.GetTranslation(settingsLang, "conf_enable_dashboard", true),
                    $"/config { message.From.Id } dashboardToggle|true"
                ));
            }

            configMenu.Add(new List<InlineKeyboardButton>());
            configMenu[depthLevel + 3].Add(InlineKeyboardButton.WithCallbackData("Close menu",
                $"/config { message.From.Id } close"));

            if (isUpdate)
            {
                Manager.BotClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    text: "*[ADMIN] [Config:]*",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(
                            configMenu
                        )
                    );
                return;
            }
            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = "*[ADMIN] Settings*\nBot configuration for this group.",
                    ReplyMarkup = new InlineKeyboardMarkup(
                            configMenu
                        )
                });
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            string[] data = callbackQuery.Data.Split(" ");
            if (data.Length < 3)
                return;
            
            int ownerId = -1;
            if(!int.TryParse(data[1], out ownerId))
            {
                return;
            }
            
            if (!Utils.BotTools.IsUserOperator(callbackQuery.From.Id) &&
                (ownerId == -1 || ownerId != callbackQuery.From.Id))
            {
                return;
            }
            
            string[] parameters = data[2].Split("|");
            if (parameters.Length < 1)
                return;
            switch (parameters[0])
            {
                case "close":
                    Manager.BotClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
                    if (parameters.Length > 1)
                        Execute(callbackQuery.Message);
                    break;
                case "getValue":
                    requestNewValue(callbackQuery, parameters[1]);
                    break;
                case "exec":
                    if (parameters.Length > 2)
                        execFunction(callbackQuery, parameters[1], parameters[2]);
                    break;
                case "dashboardToggle":
                    toggleDashboard(callbackQuery);
                    break;
                case "setLanguage":
                    changeLanguage(callbackQuery, parameters[1]);
                    break;
                default:
                    if (parameters.Length < 2)
                        return;
                    updateSetting(callbackQuery, parameters[0], parameters[1]);
                    break;
            }
        }

        private void updateSetting(
            CallbackQuery callbackQuery, string configurationParameterId, string newValue)
        {
            ConfigurationParameter config = CacheData.GroupConfigs[callbackQuery.Message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == configurationParameterId)
                .SingleOrDefault();
            if (config == null)
                return;

            // TODO - check if value is in allowed range
            CacheData.GroupConfigs[callbackQuery.Message.Chat.Id]
                [CacheData.GroupConfigs[callbackQuery.Message.Chat.Id]
                .IndexOf(config)]
                .Value = newValue;

            callbackQuery.Message.From.Id = callbackQuery.From.Id;
            Execute(callbackQuery.Message, true);
        }

        private void requestNewValue(CallbackQuery callbackQuery,
            string configurationParameterId)
        {
            Manager.BotClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
            ConfigurationParameter conf = CacheData.GroupConfigs[callbackQuery.Message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == configurationParameterId)
                .SingleOrDefault();

            if (conf == null)
                return;

            switch (conf.Type)
            {
                case "string":
                    MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = callbackQuery.Message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = $"*[ADMIN] Settings [r:{callbackQuery.Message.MessageId}]*\nProvide new value:",
                        ReplyMarkup = new ForceReplyMarkup() { Selective = true }
                    });
                    break;
                case "language":
                    MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = callbackQuery.Message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = $"*[ADMIN] Settings [r:{callbackQuery.Message.MessageId}]*\nSelect language:",
                        ReplyMarkup = new InlineKeyboardMarkup(
                                buildLanguageSelectionMenu(callbackQuery.From.Id)
                            )
                    });
                    break;
                default:
                    break;
            }
        }

        private List<List<InlineKeyboardButton>> buildLanguageSelectionMenu(long fromId)
        {
            List<List<InlineKeyboardButton>> langMenu = new List<List<InlineKeyboardButton>>();
            int btnCount = 0;
            int depthLevel = 0;
            langMenu.Add(new List<InlineKeyboardButton>());

            try
            {
                foreach (Models.Translation.Language lang in CacheData.Languages.Values)
                {
                    if (btnCount == 2)
                    {
                        btnCount = 0;
                        langMenu.Add(new List<InlineKeyboardButton>());
                        depthLevel++;
                    }

                    langMenu[depthLevel].Add(InlineKeyboardButton.WithCallbackData(
                                lang.Name,
                                $"/config { fromId } setLanguage|{ lang.LanguageId }"
                                ));

                    btnCount++;
                }
            }
            catch
            {

            }
            langMenu.Add(new List<InlineKeyboardButton>());
            langMenu[depthLevel + 1].Add(InlineKeyboardButton.WithCallbackData("Close menu", 
                $"/config { fromId } close|true"));

            return langMenu;
        }

        private void execFunction(CallbackQuery callbackQuery,
            string configurationParameterId, string newValue)
        {
            switch (configurationParameterId)
            {
                case "Gate":
                    Gate.ToggleGate(callbackQuery.Message, newValue == "true" ? true : false);
                    callbackQuery.Message.From.Id = callbackQuery.From.Id;
                    Execute(callbackQuery.Message, true);
                    break;
                case "GateSchedule":
                    Gate.ToggleSchedule(callbackQuery.Message, newValue == "true" ? true : false);
                    callbackQuery.Message.From.Id = callbackQuery.From.Id;
                    Execute(callbackQuery.Message, true);
                    break;
                default:
                    return;
            }
        }

        private void toggleDashboard(CallbackQuery callbackQuery)
        {
            Message message = callbackQuery.Message;
            DashboardUserLogic dul = new DashboardUserLogic();
            DashboardUser user = dul.GetByTelegramUserId(callbackQuery.From.Id);
            
            if (user == null)
            {
                string profilePic = "";
                var photos = Manager.BotClient.GetUserProfilePhotosAsync(callbackQuery.From.Id).Result;
                if (photos.TotalCount > 0)
                {
                    profilePic = photos.Photos[0][0].FileId;
                }

                user = dul.Add(callbackQuery.From.Id,
                    callbackQuery.From.FirstName + " " + callbackQuery.From.LastName,
                    profilePic, -2);
                if (user == null)
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            ParseMode = ParseMode.Markdown,
                            Text = "*[Report]*\n" +
                                   "Error enabling dashboard!"
                        });
                    return;
                }
            }
            
            DashboardPermissionLogic dpl = new DashboardPermissionLogic();
            DashboardPermission permission =  dpl.GetByUserId(user.DashboardUserId,
                CacheData.Groups[message.Chat.Id].GroupId);

            if (permission == null)
            {
                if (dpl.Add(CacheData.Groups[message.Chat.Id].GroupId,
                    user.DashboardUserId, DashboardPermission.Status.Active,
                    -2) == null)
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            ParseMode = ParseMode.Markdown,
                            Text = "*[Report]*\n" +
                                   "Error enabling dashboard!"
                        });
                    return;
                }
                
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = "Dashboard enabled!"
                    });
                callbackQuery.Message.From.Id = callbackQuery.From.Id;
                Execute(callbackQuery.Message, true);
                return;
            }

            if (permission.State == DashboardPermission.Status.Banned)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = "*[Report]*\n" +
                               "Error: you are banned from the dashboard!"
                    });
                return;
            }
            else if (permission.State == DashboardPermission.Status.Active)
            {
                permission.State = DashboardPermission.Status.Inactive;
            }
            else if (permission.State == DashboardPermission.Status.Inactive)
            {
                permission.State = DashboardPermission.Status.Active;
            }

            if (dpl.Update(permission, -2) == null)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = "*[Report]*\n" +
                               "Error enabling dashboard!"
                    });
            }
            else
            {   
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = "Dashboard status updated!"
                    });

                callbackQuery.Message.From.Id = callbackQuery.From.Id;
                Execute(callbackQuery.Message, true);
            }
        }

        private bool getDashboardStatus(Message message)
        {
            DashboardUserLogic dul = new DashboardUserLogic();

            DashboardUser user = dul.GetByTelegramUserId(message.From.Id);
            if (user == null)
            {
                return false;
            }
            
            DashboardPermissionLogic dpl = new DashboardPermissionLogic();
            DashboardPermission permission =  dpl.GetByUserId(user.DashboardUserId,
                CacheData.Groups[message.Chat.Id].GroupId);

            if (permission == null)
            {
                return false;
            }
            
            if (permission.State == DashboardPermission.Status.Active)
            {
                return true;
            }

            return false;
        }

        private void changeLanguage(CallbackQuery callbackQuery, string languageId)
        {
            CacheData.Groups[callbackQuery.Message.Chat.Id].SettingsLanguage = languageId;
            Execute(callbackQuery.Message, true);
        }
    }
}
