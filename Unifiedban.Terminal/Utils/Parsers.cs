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

            string username = message.From.Username != null ? "@" + message.From.Username : message.From.FirstName;
            parsedText = parsedText.Replace("{{from_username}}", username);
            parsedText = parsedText.Replace("{{from_id}}", message.From.Id.ToString());
            parsedText = parsedText.Replace("{{chat_title}}", message.Chat.Title);
            parsedText = parsedText.Replace("{{chat_id}}", message.Chat.Id.ToString());
            parsedText = parsedText.Replace("{{chat_id_noMinus}}", message.Chat.Id.ToString().Replace("-",""));
            parsedText = parsedText.Replace("{{message_id}}", message.MessageId.ToString());
            parsedText = parsedText.Replace("{{from_isBot}}", message.From.IsBot.ToString());

            if (message.ReplyToMessage != null)
            {
                string replyToUsername = message.ReplyToMessage.From.Username != null ? "@" + message.ReplyToMessage.From.Username : message.ReplyToMessage.From.FirstName;
                parsedText = parsedText.Replace("{{replyToMessage_from_username}}", replyToUsername);
                parsedText = parsedText.Replace("{{replyToMessage_from_id}}", message.ReplyToMessage.From.Id.ToString());
                parsedText = parsedText.Replace("{{replyToMessage_chat_title}}", message.ReplyToMessage.Chat.Title);
                parsedText = parsedText.Replace("{{replyToMessage_chat_id}}", message.ReplyToMessage.Chat.Id.ToString());

                if (message.ReplyToMessage.ForwardFrom != null)
                {
                    string forwardFromUsername = message.ReplyToMessage.ForwardFrom.Username != null ? "@" + message.ReplyToMessage.ForwardFrom.Username : message.ReplyToMessage.ForwardFrom.FirstName;
                    parsedText = parsedText.Replace("{{replyToMessage_forwardFrom_from_username}}", forwardFromUsername);
                    parsedText = parsedText.Replace("{{replyToMessage_forwardFrom_from_id}}", message.ReplyToMessage.ForwardFrom.Id.ToString());
                    if (message.ReplyToMessage.ForwardFromChat != null)
                    {
                        parsedText = parsedText.Replace("{{replyToMessage_forwardFrom_chat_title}}", message.ReplyToMessage.ForwardFromChat.Title);
                        parsedText = parsedText.Replace("{{replyToMessage_forwardFrom_chat_id}}", message.ReplyToMessage.ForwardFromChat.Id.ToString());
                    }
                }
            }

            if (message.ForwardFrom != null)
            {
                string forwardFromUsername = message.ReplyToMessage.From.Username != null ? "@" + message.ReplyToMessage.From.Username : message.ReplyToMessage.From.FirstName;
                parsedText = parsedText.Replace("{{forwardFrom_from_username}}", forwardFromUsername);
                parsedText = parsedText.Replace("{{forwardFrom_from_id}}", message.ReplyToMessage.From.Id.ToString());
                parsedText = parsedText.Replace("{{forwardFrom_chat_title}}", message.ReplyToMessage.Chat.Title);
                parsedText = parsedText.Replace("{{forwardFrom_chat_id}}", message.ReplyToMessage.Chat.Id.ToString());
            }

            return parsedText;
        }

        public static string VariablesParser(
            string text,
            CallbackQuery callbackQuery)
        {
            string parsedText = text;

            string username = callbackQuery.From.Username != null ? "@" + callbackQuery.From.Username : callbackQuery.From.FirstName;
            parsedText = parsedText.Replace("{{from_username}}", username);
            parsedText = parsedText.Replace("{{from_id}}", callbackQuery.From.Id.ToString());
            parsedText = parsedText.Replace("{{chat_title}}", callbackQuery.Message.Chat.Title);
            parsedText = parsedText.Replace("{{chat_id}}", callbackQuery.Message.Chat.Id.ToString());
            parsedText = parsedText.Replace("{{message_id}}", callbackQuery.Id);
            parsedText = parsedText.Replace("{{from_isBot}}", callbackQuery.From.IsBot.ToString());

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
