/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Telegram.Bot;
using Unifiedban.Models;
using Unifiedban.Models.Group;

namespace Unifiedban.Terminal
{
    public class MessageQueue
    {
        public long TelegramChatId { get; set; }
        public Queue<Models.ChatMessage> Queue { get; set; } = new Queue<Models.ChatMessage>();
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
            var msgToSend = Queue.Dequeue();
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
                    case Models.ChatMessage.PostSentActions.None:
                        break;
                    case Models.ChatMessage.PostSentActions.Pin:
                        Bot.Manager.BotClient.PinChatMessageAsync(msgToSend.Chat.Id, sent.MessageId);
                        break;
                    case Models.ChatMessage.PostSentActions.Destroy:
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
                    Message = "ChatId: " + TelegramChatId + " - " + ex.Message,
                    UserId = -1
                });
                if(ex.InnerException != null)
                    Data.Utils.Logging.AddLog(new Models.SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Unifiedban.Terminal.MessageQueue.QueueTimer_Elapsed.Send",
                        Level = Models.SystemLog.Levels.Error,
                        Message = "ChatId: " + TelegramChatId + " - " + ex.InnerException.Message,
                        UserId = -1
                    });
                
                if (ex.Message.Contains("chat not found") ||
                    ex.Message.Contains("chat was deleted") ||
                    ex.Message.Contains("bot was kicked"))
                {
                    var log = new SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Unifiedban.Terminal.MessageQueue.QueueTimer_Elapsed.Send",
                        Level = Models.SystemLog.Levels.Error,
                        Message = $"Disabling chat id: {TelegramChatId}",
                        UserId = -1
                    };
                    Data.Utils.Logging.AddLog(log);
                    //Utils.LogTools.AddSystemLog(log);
                    Utils.LogTools.AddActionLog(new ActionLog
                    {
                        ActionTypeId = "autoDisable",
                        GroupId = CacheData.Groups[TelegramChatId].GroupId,
                        Parameters = ex.Message,
                        UtcDate = DateTime.UtcNow
                    });
                    
                    CacheData.Groups[TelegramChatId].State = TelegramGroup.Status.Inactive;
                    Queue.Clear();
                }
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
