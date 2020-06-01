/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Linq;
using System.Collections.Concurrent;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using System;

namespace Unifiedban.Terminal.Bot
{
    public class CommandQueueManager
    {
        static bool isInitialized = false;
        static bool isDisposing = false;

        public static ConcurrentDictionary<long, CommandMessage> CommandsWaitingReply =
            new ConcurrentDictionary<long, CommandMessage>();

        public static void Initialize()
        {
            if (CacheData.FatalError)
                return;

            isInitialized = true;

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Command Queue Manager initialized",
                UserId = -2
            });
        }

        public static void Dispose()
        {
            isDisposing = true;
        }

        public static void EnqueueMessage(CommandMessage commandMessage)
        {
            if (!isInitialized || isDisposing)
                return;

            if (CommandsWaitingReply
                .ContainsKey(commandMessage.Message.MessageId))
                return;

            CommandsWaitingReply.TryAdd(commandMessage.Message.MessageId,
                commandMessage);
        }
        public static void DenqueueMessage(CommandMessage commandMessage)
        {
            if (!isInitialized || isDisposing)
                return;

            if (!CommandsWaitingReply
                .ContainsKey(commandMessage.Message.MessageId))
                return;

            CommandsWaitingReply.TryRemove(commandMessage.Message.MessageId,
                out CommandMessage removed);
        }

        public static void ReplyMessage(Message message)
        {
            if (!isInitialized || isDisposing)
                return;

            try
            {
                long realReplyToMessage = Convert.ToInt64(message.ReplyToMessage.Text.Split("[r:")[1].Split(']')[0]);
                if (!CommandsWaitingReply
                    .TryGetValue(realReplyToMessage,
                    out CommandMessage commandMessage))
                    return;

                switch (commandMessage.Command)
                {
                    case "AddTranslationKey":
                        Command.AddTranslation.AddTranslationKey(commandMessage, message);
                        break;
                    case "AddTranslationEntry":
                        Command.AddTranslation.AddTranslationEntry(commandMessage, message);
                        break;
                    case "SetWelcomeText":
                        if (String.IsNullOrEmpty(message.Text))
                            break;
                        if (Utils.ConfigTools.UpdateWelcomeText(message.Chat.Id, message.Text))
                        {
                            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.ReplyToMessage.MessageId);
                            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                        }
                        break;
                    case "SetRulesText":
                        if (String.IsNullOrEmpty(message.Text))
                            break;
                        if(Utils.ConfigTools.UpdateRulesText(message.Chat.Id, message.Text))
                        {
                            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.ReplyToMessage.MessageId);
                            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                        }
                        break;
                    case "Feedback":
                        if (String.IsNullOrEmpty(message.Text))
                            break;
                        DenqueueMessage(commandMessage);
                        Utils.BotTools.RecordFeedback(message);
                        break;
                    case "AddUserToBlacklist":
                        if (String.IsNullOrEmpty(message.Text))
                            break;
                        DenqueueMessage(commandMessage);
                        Utils.UserTools.AddUserToBlacklist(message.From.Id, message,
                            Convert.ToInt32(commandMessage.Value), Models.User.Banned.BanReasons.Other,
                            message.Text);
                        break;
                }
            }
            catch
            {
                return;
            }
        }
    }
}
