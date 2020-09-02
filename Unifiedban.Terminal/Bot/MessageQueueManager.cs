/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Collections.Concurrent;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Unifiedban.Models.Group;

namespace Unifiedban.Terminal.Bot
{
    public class MessageQueueManager
    {
        static bool isInitialized = false;
        static bool isDisposing = false;

        public static ConcurrentDictionary<long, MessageQueue> PrivateChats =
            new ConcurrentDictionary<long, MessageQueue>();
        public static ConcurrentDictionary<long, MessageQueue> GroupChats =
            new ConcurrentDictionary<long, MessageQueue>();

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
                Message = "Message Queue Manager initialized",
                UserId = -2
            });
        }

        public static void Dispose()
        {
            isDisposing = true;
            // Wait until all queues are dispatched
            while (PrivateChats.Values
                .Where(x => x.Queue.Count > 0)
                .ToList().Count > 0
                ||
                GroupChats.Values
                .Where(x => x.Queue.Count > 0)
                .ToList().Count > 0) ; ;
        }

        public static bool AddGroupIfNotPresent(TelegramGroup group)
        {
            // Do not accept new chat if going to shutdown
            if (isDisposing)
                return false;

            if (GroupChats.ContainsKey(group.TelegramChatId))
                return false;

            bool added = GroupChats.TryAdd(group.TelegramChatId,
                new MessageQueue(group.TelegramChatId, 20));
            return added;
        }

        public static bool RemoveGroupIfNotPresent(TelegramGroup group)
        {
            // Do not accept new chat if going to shutdown
            if (isDisposing)
                return false;

            if (!GroupChats.ContainsKey(group.TelegramChatId))
                return false;

            bool removed = GroupChats.TryRemove(group.TelegramChatId, out MessageQueue messageQueue);
            return removed;
        }

        public static bool RemoveGroupIfNotPresent(long telegramChatId)
        {
            // Do not accept new chat if going to shutdown
            if (isDisposing)
                return false;

            if (!GroupChats.ContainsKey(telegramChatId))
                return false;

            bool removed = GroupChats.TryRemove(telegramChatId, out MessageQueue messageQueue);
            return removed;
        }

        public static bool AddChatIfNotPresent(long chatId)
        {
            // Do not accept new chat if going to shutdown
            if (isDisposing)
                return false;

            if (PrivateChats.ContainsKey(chatId))
                return false;

            bool added = PrivateChats.TryAdd(chatId, new MessageQueue(chatId, 60));
            return added;
        }

        public static void EnqueueMessage(Models.ChatMessage message)
        {
            if (!isInitialized || isDisposing)
                return;

            if (message.Chat.Type == ChatType.Group
                || message.Chat.Type == ChatType.Supergroup)
            {
                if (!GroupChats.ContainsKey(message.Chat.Id))
                {
                    return;
                }
                GroupChats[message.Chat.Id]
                    .Queue
                    .Enqueue(message);
            }
            if (message.Chat.Type == ChatType.Private
                || message.Chat.Type == ChatType.Channel)
            {
                if (!PrivateChats.ContainsKey(message.Chat.Id))
                {
                    PrivateChats.TryAdd(message.Chat.Id, new MessageQueue(message.Chat.Id, 60));
                }
                    PrivateChats[message.Chat.Id]
                    .Queue
                    .Enqueue(message);
            }
        }
        public static void EnqueueLog(Models.ChatMessage message)
        {
            if (!isInitialized || isDisposing)
                return;

#if DEBUG
            message.Text = message.Text.Replace("#UB", "UBB");
#endif

            message.Chat = new Chat()
            {
                Id = CacheData.ControlChatId,
                Type = ChatType.Channel,
            };
            message.DisableWebPagePreview = true;

            PrivateChats[CacheData.ControlChatId]
                    .Queue
                    .Enqueue(message);
        }
    }
}
