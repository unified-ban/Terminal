using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

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
            if (msgToSend == null)
                return;
            try
            {
                Telegram.Bot.Types.Message sent = Bot.Manager.BotClient.SendTextMessageAsync(
                        chatId: TelegramChatId,
                        text: msgToSend.Text,
                        parseMode: msgToSend.ParseMode,
                        disableWebPagePreview: msgToSend.DisableWebPagePreview,
                        disableNotification: msgToSend.DisableNotification,
                        replyToMessageId: msgToSend.ReplyToMessageId,
                        replyMarkup: msgToSend.ReplyMarkup
                    ).Result;

                switch (msgToSend.PostSentAction)
                {
                    default:
                    case ChatMessage.PostSentActions.None:
                        break;
                    case ChatMessage.PostSentActions.Pin:
                        Bot.Manager.BotClient.PinChatMessageAsync(msgToSend.Chat.Id, sent.MessageId);
                        break;
                    case ChatMessage.PostSentActions.Destroy:
                        Task.Run(() =>
                        {
                            System.Threading.Thread.Sleep(1000 * msgToSend.AutoDestroyTimeInSeconds);
                            Bot.Manager.BotClient.DeleteMessageAsync(msgToSend.Chat.Id, sent.MessageId);
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Terminal.MessageQueue.QueueTimer_Elapsed.Send",
                    Level = Models.SystemLog.Levels.Error,
                    Message = ex.Message,
                    UserId = -1
                });
                if(ex.InnerException != null)
                    Data.Utils.Logging.AddLog(new Models.SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Unifiedban.Terminal.MessageQueue.QueueTimer_Elapsed.Send",
                        Level = Models.SystemLog.Levels.Error,
                        Message = ex.InnerException.Message,
                        UserId = -1
                    });
            }

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
