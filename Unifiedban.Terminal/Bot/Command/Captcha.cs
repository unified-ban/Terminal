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


namespace Unifiedban.Terminal.Bot.Command
{
    public class Captcha : ICommand
    {
        public void Execute(Message message) { }

        public void Execute(CallbackQuery callbackQuery)
        {
            string[] args = callbackQuery.Data.Split(" ");
            long captchaId = Convert.ToInt64(args[1]);

            if (captchaId != callbackQuery.From.Id)
                return;

            if (args.Length > 2)
            {
                if (CacheData.CaptchaAutoKickTimers.ContainsKey(args[2]))
                {
                    CacheData.CaptchaAutoKickTimers[args[2]].Stop();
                    CacheData.CaptchaAutoKickTimers.TryRemove(args[2], out var timer);
                }
            }

            Manager.BotClient.DeleteMessageAsync(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId);

            Manager.BotClient.RestrictChatMemberAsync(
                    callbackQuery.Message.Chat.Id,
                    callbackQuery.From.Id,
                    Manager.BotClient.GetChatAsync(callbackQuery.Message.Chat.Id).Result.Permissions);
            
            if (CacheData.CaptchaStrikes.ContainsKey(callbackQuery.From.Id))
            {
                CacheData.CaptchaStrikes.Remove(callbackQuery.From.Id);
            }

            string name = callbackQuery.From.Username != null ? "@" + callbackQuery.From.Username : callbackQuery.From.FirstName;

            bool welcomeMessage = false;
            ConfigurationParameter welcomeMessageConfig = CacheData.GroupConfigs[callbackQuery.Message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "WelcomeMessage")
                .FirstOrDefault();
            if (welcomeMessageConfig != null)
                if (welcomeMessageConfig.Value.ToLower() == "true")
                    welcomeMessage = true;

            if (welcomeMessage)
            {
                BusinessLogic.ButtonLogic buttonLogic = new BusinessLogic.ButtonLogic();
                List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();
                foreach (Button btn in buttonLogic
                    .GetByChat(CacheData.Groups[callbackQuery.Message.Chat.Id]
                    .GroupId))
                {
                    buttons.Add(new List<InlineKeyboardButton>());
                    buttons[buttons.Count -1].Add(InlineKeyboardButton.WithUrl(btn.Name, btn.Content));
                }

                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = callbackQuery.Message.Chat,
                       ParseMode = ParseMode.Html,
                       Text = Utils.Parsers.VariablesParser(
                           CacheData.Groups[callbackQuery.Message.Chat.Id].WelcomeText,
                           callbackQuery),
                       ReplyMarkup = new InlineKeyboardMarkup(
                            buttons
                        )
                   });

                return;
            }

            MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = callbackQuery.Message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = CacheData.GetTranslation(CacheData.Groups[callbackQuery.Message.Chat.Id].SettingsLanguage, 
                            "captcha_ok", true).Replace("{{name}}", name)
                    });

        }
    }
}
