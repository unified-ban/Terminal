using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Unifiedban.Terminal
{
    public class MessageQueue
    {
        public long TelegramChatId { get; set; }
        public Queue<ChatMessage> Queue { get; set; } = new Queue<ChatMessage>();
        public DateTime FirstMessageUtc { get; set; }
        public DateTime LastMessageUtc { get; set; }
        public int LastMinuteMessagesCount { get; set; } = 0;
        public short MaxMessagePerMinute { get; set; }
        public System.Timers.Timer QueueTimer { get; set; } =
            new System.Timers.Timer(100);

        private bool handlingInProgress = false;

        public MessageQueue(long telegramChatId, short maxMsgPerMinute)
        {
            TelegramChatId = telegramChatId;
            MaxMessagePerMinute = maxMsgPerMinute;

            FirstMessageUtc = DateTime.UtcNow;
            LastMessageUtc = DateTime.UtcNow;

            QueueTimer = new System.Timers.Timer(100);
            QueueTimer.Elapsed += QueueTimer_Elapsed;
            QueueTimer.AutoReset = true;
            QueueTimer.Start();
        }

        private void QueueTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Check if there is any message in queue
            if (Queue.Count == 0)
                return;
            if (handlingInProgress)
                return;

            handlingInProgress = true;
            bool doResetCount = false;
             while (DateTime.UtcNow
                    .Subtract(FirstMessageUtc)
                    .Minutes < 1
                && LastMinuteMessagesCount >= MaxMessagePerMinute)
            {
                System.Threading.Thread.Sleep(100);
                doResetCount = true;
            }

            // Take next message from the queue and send it
            ChatMessage msgToSend = Queue.Dequeue();
            Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: msgToSend.Chat.Id,
                    text: msgToSend.Text,
                    parseMode: msgToSend.ParseMode,
                    disableWebPagePreview: msgToSend.DisableWebPagePreview,
                    disableNotification: msgToSend.DisableNotification,
                    replyToMessageId: msgToSend.ReplyToMessageId,
                    replyMarkup: msgToSend.ReplyMarkup
                );

            // Reset counter and time if we waited previously
            if (doResetCount)
            {
                LastMinuteMessagesCount = 1;
                FirstMessageUtc = DateTime.UtcNow;
            }
            else
                LastMinuteMessagesCount += 1;

            LastMessageUtc = DateTime.UtcNow;
            handlingInProgress = false;
        }
    }
}
