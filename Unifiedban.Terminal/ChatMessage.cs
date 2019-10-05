
using System;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal
{
    public class ChatMessage
    {
        public enum PostSentActions
        {
            None = 0,
            Pin = 1,
            Destroy = 2
        }

        public Chat Chat { get; set; }
        public string Text { get; set; }
        public ParseMode ParseMode { get; set; } = ParseMode.Default;
        public bool DisableWebPagePreview { get; set; } = false;
        public bool DisableNotification { get; set; } = false;
        public int ReplyToMessageId { get; set; } = 0;
        public IReplyMarkup ReplyMarkup { get; set; } = null;
        public DateTime Timestamp { get; set; }
        public PostSentActions PostSentAction { get; set; } = PostSentActions.None;
        public ushort AutoDestroyTimeInSeconds { get; set; } = 10;
    }
}
