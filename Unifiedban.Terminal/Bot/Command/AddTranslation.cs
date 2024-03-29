﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Unifiedban.Models.Translation;

namespace Unifiedban.Terminal.Bot.Command
{
    public class AddTranslation : ICommand
    {
        public void Execute(Message message)
        {
            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == message.From.Id
                && x.Level == Models.Operator.Levels.Super) == null)
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "*[Report]*\n" +
                        "⚠️ Non operator tried to use /addtranslation\n" +
                        "\n*Chat:* {0}" +
                        "\n*ChatId:* {1}" +
                        "\n*UserId:* {2}" +
                        "\n\n*hash_code:* #UB{3}-{4}",
                        message.Chat.Title,
                        message.Chat.Id,
                        message.From.Id,
                        message.Chat.Id.ToString().Replace("-",""),
                        Guid.NewGuid())
                );
                return;
            }

            try
            {
                BusinessLogic.TranslationLogic translationLogic = new BusinessLogic.TranslationLogic();
                List<Language> languages = translationLogic.GetLanguage();
                List<InlineKeyboardButton> langButtons = new List<InlineKeyboardButton>();
                foreach(Language lang in languages)
                {
                    langButtons.Add(InlineKeyboardButton.WithCallbackData(
                        $"{lang.Name} ({lang.LanguageId})",
                        $"/AddTranslation language:{lang.LanguageId}"
                        ));
                }

                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ReplyToMessageId = message.MessageId,
                        ParseMode = ParseMode.Markdown,
                        Text = "*[ADMIN]*\nSelect language:",
                        ReplyMarkup = new InlineKeyboardMarkup(
                            langButtons
                        )
                    });
            }
            catch (Exception ex)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Terminal.Command.AddTranslation",
                    Level = Models.SystemLog.Levels.Error,
                    Message = ex.Message,
                    UserId = -1
                });

                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    text: "Error. Check logs."
                );
            }
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == callbackQuery.From.Id
                    && x.Level == Models.Operator.Levels.Super) == null)
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = callbackQuery.Message.Chat,
                       ReplyToMessageId = callbackQuery.Message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "*[Report]*\n" +
                        "⚠️ Non operator tried to use /addtranslation\n" +
                        "\n*Chat:* {0}" +
                        "\n*ChatId:* {1}" +
                        "\n*UserId:* {2}" +
                        "\n\n*hash_code:* #UB{3}-{4}",
                        callbackQuery.Message.Chat.Title,
                        callbackQuery.Message.Chat.Id,
                        callbackQuery.From.Id,
                        callbackQuery.Message.Chat.Id.ToString().Replace("-",""),
                        Guid.NewGuid())
                );
                return;
            }

            string data = callbackQuery.Data.Replace("/AddTranslation ", "");
            string command = data.Split(':')[0];
            string value = data.Split(':')[1];

            if(command == "language")
            {
                Manager.BotClient.DeleteMessageAsync(
                    callbackQuery.Message.Chat,
                    callbackQuery.Message.MessageId);
                CommandMessage commandMessage = new CommandMessage()
                {
                    Command = "AddTranslationKey",
                    Value = value,
                    Message = callbackQuery.Message,
                    Timestamp = DateTime.UtcNow
                };
                CommandQueueManager.EnqueueMessage(commandMessage);

                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = callbackQuery.Message.Chat,
                        ReplyToMessageId = callbackQuery.Message.ReplyToMessage.MessageId,
                        ParseMode = ParseMode.Markdown,
                        Text = $"*[ADMIN] [r:{callbackQuery.Message.MessageId}]*\nDeclare KeyId:",
                        ReplyMarkup = new ForceReplyMarkup() { Selective = true }
                    });
            }
        }

        public static void AddTranslationKey(CommandMessage commandMessage,
            Message replyMessage)
        {
            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId ==
                    commandMessage.Message.ReplyToMessage.From.Id
                && x.Level == Models.Operator.Levels.Super) == null)
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = replyMessage.Chat,
                       ReplyToMessageId = replyMessage.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "User *{0}:{1}* tried to use command AddTranslationKey.",
                        replyMessage.From.Id,
                        replyMessage.From.Username),
                    replyMarkup: new ReplyKeyboardRemove() { Selective = true }
                );
                return;
            }

            CommandQueueManager.DenqueueMessage(commandMessage);

            var regexItem = new Regex("^[a-zA-Z0-9_]*$");
            if (!regexItem.IsMatch(replyMessage.Text.Trim()))
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = replyMessage.Chat,
                        ReplyToMessageId = replyMessage.MessageId,
                        ParseMode = ParseMode.Markdown,
                        Text = "Error: translation key cannot contain space and/or special chars.\n"
                            + "Start again the process.",
                        ReplyMarkup = new ReplyKeyboardRemove() { Selective = true }
                    });
                return;
            }

            BusinessLogic.TranslationLogic translationLogic = new BusinessLogic.TranslationLogic();
            Key translationKey = translationLogic.GetKeyById(replyMessage.Text.Trim());
            if(translationKey == null)
                translationKey = translationLogic.AddKey(
                    replyMessage.Text.Trim(),
                    -1);

            if (translationKey == null)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = replyMessage.Chat,
                        ReplyToMessageId = replyMessage.MessageId,
                        ParseMode = ParseMode.Markdown,
                        Text = "*Error* adding translation key.\nCheck internal logs.",
                        ReplyMarkup = new ReplyKeyboardRemove() { Selective = true }
                    });
                return;
            }

            Entry entryExists = translationLogic.GetEntryById(commandMessage.Value, translationKey.KeyId);
            if(entryExists != null)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = replyMessage.Chat,
                        ReplyToMessageId = replyMessage.MessageId,
                        ParseMode = ParseMode.Markdown,
                        Text = $"*Error*\n {commandMessage.Value} translation for key `{translationKey.KeyId}` already exists!",
                        ReplyMarkup = new ReplyKeyboardRemove() { Selective = true }
                    });
                return;
            }

            CommandMessage newCommandMessage = new CommandMessage()
            {
                Command = "AddTranslationEntry",
                Value = commandMessage.Value + "|" + translationKey.KeyId,
                Message = replyMessage,
                Timestamp = DateTime.UtcNow
            };
            CommandQueueManager.EnqueueMessage(newCommandMessage);

            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = replyMessage.Chat,
                    ReplyToMessageId = replyMessage.MessageId,
                    ParseMode = ParseMode.Markdown,
                    Text = $"*[ADMIN] [r:{replyMessage.MessageId}]*\nType translation for " +
                    $"`{translationKey.KeyId}` in `{CacheData.Languages[commandMessage.Value].Name}`:",
                    ReplyMarkup = new ForceReplyMarkup() { Selective = true }
                });
        }
        public static void AddTranslationEntry(CommandMessage commandMessage,
            Message replyMessage)
        {
            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == 
                commandMessage.Message.From.Id
                && x.Level == Models.Operator.Levels.Super) == null)
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = replyMessage.Chat,
                       ReplyToMessageId = replyMessage.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "User *{0}:{1}* tried to use command AddTranslationEntry.",
                        replyMessage.From.Id,
                        replyMessage.From.Username)
                );
                return;
            }

            string[] parameters = commandMessage.Value.Split('|');
            if(parameters.Length != 2)
            {
                CommandQueueManager.DenqueueMessage(commandMessage);
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = replyMessage.Chat,
                       ReplyToMessageId = replyMessage.MessageId,
                       Text = CacheData.GetTranslation("en", "error_invalid_parameters"),
                       ReplyMarkup = new ReplyKeyboardRemove() { Selective = true }
                   });
            }
            string languageId = parameters[0];
            string keyId = parameters[1];

            BusinessLogic.TranslationLogic translationLogic = new BusinessLogic.TranslationLogic();
            Entry translationEntry = translationLogic.AddEntry(
                languageId, keyId,
                replyMessage.Text, -1);

            if (translationEntry == null)
            {
                CommandQueueManager.DenqueueMessage(commandMessage);
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = replyMessage.Chat,
                        ReplyToMessageId = replyMessage.MessageId,
                        ParseMode = ParseMode.Markdown,
                        Text = "*Error* adding translation entry.\nCheck internal logs.",
                        ReplyMarkup = new ReplyKeyboardRemove() { Selective = true }
                    });
                return;
            }

            CommandQueueManager.DenqueueMessage(commandMessage);
            MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = replyMessage.Chat,
                        ReplyToMessageId = replyMessage.MessageId,
                        ParseMode = ParseMode.Markdown,
                        Text = "*OK!*\nTranslation added successfully!\nRemember to reload them manually!",
                        ReplyMarkup = new ReplyKeyboardRemove() { Selective = true }
                    });
        }
    }
}
