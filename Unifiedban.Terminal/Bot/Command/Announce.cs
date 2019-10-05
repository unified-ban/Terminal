using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Announce : ICommand
    {
        public void Execute(Message message)
        {
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
                Manager.BotClient.SendTextMessageAsync(
                    chatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "User *{0}:{1}* tried to use command Check.",
                        message.From.Id,
                        message.From.Username)
                );
                return;
            }

            string command = message.Text.Split(" ")[0].Remove(0, 1);
            if (command.Contains("@"))
            {
                if (!String.Equals(command.Split("@")[1],
                    Manager.Username, StringComparison.CurrentCultureIgnoreCase))
                    return;
                command = command.Split("@")[0];
            }
            message.Text = message.Text.Remove(0, command.Length);

            CommandMessage commandMessage = new CommandMessage()
            {
                Command = "PinAnnounce",
                Message = message,
                Timestamp = DateTime.UtcNow
            };
            CommandQueueManager.EnqueueMessage(commandMessage);

            string messageHeader = CacheData.GetTranslation("en", "command_announce_header");
            string parsedMessage = messageHeader + "\n" + message.Text;
            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Html,
                    Text = parsedMessage
                });
        }

        public void Execute(CallbackQuery callbackQuery) { }

        public static void PinAnnounce(Message message)
        {
            int charsToRemove = message.Text.Split(']')[0].Length;
            Manager.BotClient.EditMessageTextAsync(message.Chat.Id, message.MessageId, message.Text.Remove(0, charsToRemove));
            Manager.BotClient.PinChatMessageAsync(message.Chat.Id, message.MessageId);
        }
    }
}
