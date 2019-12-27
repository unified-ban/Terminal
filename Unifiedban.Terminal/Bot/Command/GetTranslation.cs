/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Unifiedban.Models.Translation;

namespace Unifiedban.Terminal.Bot.Command
{
    public class GetTranslation : ICommand
    {
        public void Execute(Message message)
        {
            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == message.From.Id
                && x.Level == Models.Operator.Levels.Super) == null)
            {
                MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                Manager.BotClient.SendTextMessageAsync(
                    chatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "User *{0}:{1}* tried to use command GetTranslation.",
                        message.From.Id,
                        message.From.Username)
                );
                return;
            }

            if (message.Text.Split(" ").Length != 2)
            {
                MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       ParseMode = ParseMode.Markdown,
                       Text = "Error: missing KeyId to search.\nUsage: /GetTranslation KeyId"
                   });
                return;
            }

            try
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ReplyToMessageId = message.MessageId,
                        ParseMode = ParseMode.Markdown,
                        Text = "*[ADMIN]*\nSelect data source:",
                        ReplyMarkup = new InlineKeyboardMarkup(
                            new List<InlineKeyboardButton>()
                            {
                                InlineKeyboardButton.WithCallbackData(
                                    "Memory",
                                    "/GetTranslation InMemory|" + message.Text.Split(" ")[1]
                                    ),
                                InlineKeyboardButton.WithCallbackData(
                                    "Database",
                                    "/GetTranslation Database|" + message.Text.Split(" ")[1]
                                    )
                            }
                        )
                    });
            }
            catch (Exception ex)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Terminal.Command.GetTranslation",
                    Level = Models.SystemLog.Levels.Error,
                    Message = ex.Message,
                    UserId = -1
                });

                Manager.BotClient.SendTextMessageAsync(
                    chatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                    text: "Error. Check logs."
                );
            }

        }

        public void Execute(CallbackQuery callbackQuery)
        {
            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId ==
                    callbackQuery.Message.ReplyToMessage.From.Id
                && x.Level == Models.Operator.Levels.Super) == null)
            {
                MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = callbackQuery.Message.Chat,
                       ReplyToMessageId = callbackQuery.Message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                Manager.BotClient.SendTextMessageAsync(
                    chatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "User *{0}:{1}* tried to use command GetTranslation.",
                        callbackQuery.Message.From.Id,
                        callbackQuery.Message.From.Username)
                );
                return;
            }

            string[] parameters = callbackQuery.Data.Split(" ");
            string dataSource = parameters[1].Split('|')[0];
            string keyId = parameters[1].Split('|')[1].Trim();

            List<Entry> entries = new List<Entry>();

            if (dataSource == "InMemory")
            {
                foreach (string language in CacheData.Translations.Keys)
                {
                    bool isTranslated = CacheData.Translations[language].TryGetValue(keyId, out Entry entry);
                    if (isTranslated)
                        entries.Add(entry);

                }

                if (entries.Count == 0)
                    MessageQueueManager.EnqueueMessage(
                       new ChatMessage()
                       {
                           Timestamp = DateTime.UtcNow,
                           Chat = callbackQuery.Message.Chat,
                           ReplyToMessageId = callbackQuery.Message.MessageId,
                           Text = "Translation key exists but not translation is present."
                       });
            }
            else if (dataSource == "Database")
            {
                BusinessLogic.TranslationLogic translationLogic = new BusinessLogic.TranslationLogic();
                Key translationKey = translationLogic.GetKeyById(keyId);
                if (translationKey == null)
                    MessageQueueManager.EnqueueMessage(
                       new ChatMessage()
                       {
                           Timestamp = DateTime.UtcNow,
                           Chat = callbackQuery.Message.Chat,
                           ReplyToMessageId = callbackQuery.Message.MessageId,
                           Text = "This translation key does not exist."
                       });

                entries = translationLogic.GetEntriesById(keyId);
                if (entries.Count == 0)
                    MessageQueueManager.EnqueueMessage(
                       new ChatMessage()
                       {
                           Timestamp = DateTime.UtcNow,
                           Chat = callbackQuery.Message.Chat,
                           ReplyToMessageId = callbackQuery.Message.MessageId,
                           Text = "Translation key exists but not translation is present."
                       });
            }

            string answer = $"Available {dataSource} translations:";
            foreach(Entry translation in entries)
            {
                answer += $"\n*{translation.Language.Name}* : {translation.Translation}";
            }
            MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = callbackQuery.Message.Chat,
                       ReplyToMessageId = callbackQuery.Message.MessageId,
                       ParseMode = ParseMode.Markdown,
                       Text = answer
                   });
        }
    }
}
