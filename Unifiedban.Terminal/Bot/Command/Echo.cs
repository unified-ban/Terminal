using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Unifiedban.Models.Translation;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Echo : ICommand
    {
        public void Execute(Message message)
        {
            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == message.From.Id
                && x.Level == Models.Operator.Levels.Super) == null)
            {
                MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                MessageQueueManager.EnqueueLog(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                return;
            }

            if(message.ReplyToMessage == null)
            {
                MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = "This command works only as reply to an exsisting message"
                   });
                return;
            }

            MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       ParseMode = ParseMode.Markdown,
                       Text = Utils.Parsers.VariablesParser(message.ReplyToMessage.Text, message)
                   });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
