using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Unmute : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            var sender = message.SenderChat?.Id ?? message.From?.Id ?? 0;
            var isOperator = Utils.BotTools.IsUserOperator(sender, Models.Operator.Levels.Basic);
            var isAdmin = Utils.ChatTools.IsUserAdmin(message.Chat.Id, sender);
            if (!isOperator && !isAdmin)
            {
                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ReplyToMessageId = message.MessageId,
                        Text = CacheData.GetTranslation("en", "error_not_auth_command")
                    });
                return;
            }
            else if (!isOperator && isAdmin)
            {
                var adminPermissions = CacheData.ChatAdmins[message.Chat.Id][sender];
                if (!adminPermissions.CanRestrictMembers)
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = message.Chat,
                            Text = CacheData.GetTranslation("en", "error_not_auth_command")
                        });
                    return;
                }
            }

            long userId = 0;
            if (message.ReplyToMessage != null)
            {
                userId = message.ReplyToMessage.From.Id;
            }
            else if (message.Text.Contains(" "))
            {
                long.TryParse(message.Text.Split(" ")[1], out userId);
            }

            if (userId == 0)
            {
                MessageQueueManager.EnqueueMessage(
                   new Models.ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ParseMode = ParseMode.Markdown,
                       Text = CacheData.GetTranslation("en", "command_unmute_missingMessage")
                   });
                return;
            }

            Manager.BotClient.RestrictChatMemberAsync(
                    message.Chat.Id,
                    userId,
                    new ChatPermissions()
                    {
                        CanSendMessages = true,
                        CanAddWebPagePreviews = true,
                        CanChangeInfo = true,
                        CanInviteUsers = true,
                        CanPinMessages = true,
                        CanSendMediaMessages = true,
                        CanSendOtherMessages = true,
                        CanSendPolls = true
                    });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
