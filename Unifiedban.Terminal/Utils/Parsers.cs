using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Utils
{
    public class Parsers
    {
        public static string VariablesParser(string text, Message message)
        {
            string parsedText = text;

            parsedText = parsedText.Replace("{{from_username}}", message.From.Username);
            parsedText = parsedText.Replace("{{from_id}}", message.From.Username);
            parsedText = parsedText.Replace("{{chat_title}}", message.Chat.Title);
            parsedText = parsedText.Replace("{{chat_id}}", message.Chat.Id.ToString());

            if (message.ReplyToMessage != null)
            {
                parsedText = parsedText.Replace("{{replyToMessage_from_username}}", message.ReplyToMessage.From.Username);
                parsedText = parsedText.Replace("{{replyToMessage_from_id}}", message.ReplyToMessage.From.Username);
                parsedText = parsedText.Replace("{{replyToMessage_chat_title}}", message.ReplyToMessage.Chat.Title);
                parsedText = parsedText.Replace("{{replyToMessage_chat_id}}", message.ReplyToMessage.Chat.Id.ToString());
            }

            return parsedText;
        }
    }
}
