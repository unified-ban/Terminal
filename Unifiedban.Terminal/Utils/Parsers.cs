using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Utils
{
    public class Parsers
    {
        public static string VariablesParser(
            string text,
            Message message)
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

        public static string VariablesParser(
            string text,
            string languageId = "en")
        {
            string parsedText = text;

            parsedText = parsedText.Replace("{{command_check_first_row}}",
                CacheData.GetTranslation(languageId, "command_check_first_row"));
            parsedText = parsedText.Replace("{{privilege_delete_messages}}",
                CacheData.GetTranslation(languageId, "privilege_delete_messages"));
            parsedText = parsedText.Replace("{{privilege_ban_users}}",
                CacheData.GetTranslation(languageId, "privilege_ban_users"));
            parsedText = parsedText.Replace("{{privilege_pin_messages}}",
                CacheData.GetTranslation(languageId, "privilege_pin_messages"));
            parsedText = parsedText.Replace("{{optional}}",
                CacheData.GetTranslation(languageId, "optional"));
            parsedText = parsedText.Replace("{{result}}",
                CacheData.GetTranslation(languageId, "result"));
            parsedText = parsedText.Replace("{{privilege_check_ok}}",
                CacheData.GetTranslation(languageId, "privilege_check_ok"));
            parsedText = parsedText.Replace("{{privilege_check_ko}}",
                CacheData.GetTranslation(languageId, "privilege_check_ko"));
            parsedText = parsedText.Replace("{{true}}",
                CacheData.GetTranslation(languageId, "true"));
            parsedText = parsedText.Replace("{{false}}",
                CacheData.GetTranslation(languageId, "false"));

            return parsedText;
        }
    }
}
