using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot
{
    public class MessageQueueManager
    {
        public static ConcurrentDictionary<long, QueueMonitor> PrivateChats = new ConcurrentDictionary<long, QueueMonitor>();
        public static ConcurrentDictionary<long, QueueMonitor> GroupChats = new ConcurrentDictionary<long, QueueMonitor>();

        public static void Initialize()
        {

        }

        public static void SendTextMessage(
            Chat chat,
            string text,
            ParseMode parseMode = ParseMode.Default,
            bool disableWebPagePreview = false,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            QueueMonitor qm;

            if (chat.Type == ChatType.Private
                || chat.Type == ChatType.Channel)
            {
                if (!PrivateChats
                    .TryGetValue(chat.Id, out qm))
                {
                    Manager.BotClient.SendTextMessageAsync(
                        chatId: chat.Id,
                        text: text,
                        parseMode: parseMode,
                        disableWebPagePreview: disableWebPagePreview,
                        disableNotification: disableNotification,
                        replyToMessageId: replyToMessageId,
                        replyMarkup: replyMarkup
                    );
                    qm = new QueueMonitor()
                    {
                        ChatId = chat.Id,
                        Type = chat.Type,
                        LastMessageUtc = DateTime.UtcNow,
                        LastMinuteMessagesCount = 1
                    };
                    PrivateChats.TryAdd(chat.Id, qm);
                }

                double minutesDifference = DateTime.UtcNow.Subtract(qm.LastMessageUtc).TotalMinutes;
                bool doResetCount = false;
                while (minutesDifference < 1
                    && qm.LastMinuteMessagesCount >= 60)
                {
                    System.Threading.Thread.Sleep(100);
                    doResetCount = true;
                }

                Manager.BotClient.SendTextMessageAsync(
                        chatId: chat.Id,
                        text: text,
                        parseMode: parseMode,
                        disableWebPagePreview: disableWebPagePreview,
                        disableNotification: disableNotification,
                        replyToMessageId: replyToMessageId,
                        replyMarkup: replyMarkup
                    );

                qm.LastMessageUtc = DateTime.UtcNow;
                if (doResetCount)
                    qm.LastMinuteMessagesCount = 1;
                else
                    qm.LastMinuteMessagesCount += 1;
                PrivateChats[chat.Id] = qm;
                return;
            }

            if (chat.Type == ChatType.Group
                || chat.Type == ChatType.Supergroup)
            {
                if (!GroupChats
                    .TryGetValue(chat.Id, out qm))
                {
                    Manager.BotClient.SendTextMessageAsync(
                        chatId: chat.Id,
                        text: text,
                        parseMode: parseMode,
                        disableWebPagePreview: disableWebPagePreview,
                        disableNotification: disableNotification,
                        replyToMessageId: replyToMessageId,
                        replyMarkup: replyMarkup
                    );
                    qm = new QueueMonitor()
                    {
                        ChatId = chat.Id,
                        Type = chat.Type,
                        LastMessageUtc = DateTime.UtcNow,
                        LastMinuteMessagesCount = 1
                    };
                    GroupChats.TryAdd(chat.Id, qm);
                }

                double minutesDifference = DateTime.UtcNow.Subtract(qm.LastMessageUtc).TotalMinutes;
                bool doResetCount = false;
                while (minutesDifference < 1
                    && qm.LastMinuteMessagesCount >= 20)
                {
                    System.Threading.Thread.Sleep(100);
                    doResetCount = true;
                }

                Manager.BotClient.SendTextMessageAsync(
                        chatId: chat.Id,
                        text: text,
                        parseMode: parseMode,
                        disableWebPagePreview: disableWebPagePreview,
                        disableNotification: disableNotification,
                        replyToMessageId: replyToMessageId,
                        replyMarkup: replyMarkup
                    );

                qm.LastMessageUtc = DateTime.UtcNow;
                if(doResetCount)
                    qm.LastMinuteMessagesCount = 1;
                else
                    qm.LastMinuteMessagesCount += 1;
                GroupChats[chat.Id] = qm;
                return;
            }
        }

        public class QueueMonitor
        {
            public long ChatId { get; set; }
            public ChatType Type { get; set; }
            public DateTime LastMessageUtc { get; set; }
            public int LastMinuteMessagesCount { get; set; }
        }
    }
}
