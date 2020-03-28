using System;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Kick : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            if (!Utils.BotTools.IsUserOperator(message.From.Id) &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                return;
            }

            int userToKick = 0;
            if (message.ReplyToMessage != null)
            {
                userToKick = message.ReplyToMessage.From.Id;
            }
            else if (message.Text.Contains(" "))
            {
                int.TryParse(message.Text.Split(" ")[1], out userToKick);
            }
            
            if(userToKick == 0)
            {
                MessageQueueManager.EnqueueMessage(
               new ChatMessage()
               {
                   Timestamp = DateTime.UtcNow,
                   Chat = message.Chat,
                   ParseMode = ParseMode.Markdown,
                   Text = CacheData.GetTranslation("en", "command_kick_missingMessage")
               });
                return;
            }

            Manager.BotClient.KickChatMemberAsync(message.Chat.Id, userToKick);
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
