using System;
using System.Linq;
using System.Collections.Concurrent;
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

        public static void EnqueueMessage(ChatMessage message)
        {
            if (!isInitialized || isDisposing)
                return;

            if (message.Chat.Type == ChatType.Group
                || message.Chat.Type == ChatType.Supergroup)
            {
                if (!GroupChats.ContainsKey(message.Chat.Id))
                    return;
                GroupChats[message.Chat.Id]
                    .Queue
                    .Enqueue(message);
            }
            if (message.Chat.Type == ChatType.Private
                || message.Chat.Type == ChatType.Channel)
            {
                if (!PrivateChats.ContainsKey(message.Chat.Id))
                    return;
                PrivateChats[message.Chat.Id]
                    .Queue
                    .Enqueue(message);
            }
        }
    }
}
