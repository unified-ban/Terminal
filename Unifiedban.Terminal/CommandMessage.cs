using System;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal
{
    public class CommandMessage

    {
        public Message Message { get; set; }
        public string Command { get; set; }
        public string Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
