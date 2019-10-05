using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Check : ICommand
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

            bool canRestrictMembers = false;
            bool canDeleteMessages = false;
            bool canPinMessages = false;
            string text = CacheData.SysConfigs
                .Single(x => x.SysConfigId == "CommandCheckKoText").Value;

            var me = Manager.BotClient.GetChatMemberAsync(message.Chat.Id, Manager.MyId).Result;
            if (me.CanRestrictMembers != null)
                canRestrictMembers = (bool)me.CanRestrictMembers;
            if (me.CanDeleteMessages != null)
                canDeleteMessages = (bool)me.CanDeleteMessages;
            if (me.CanPinMessages != null)
                canPinMessages = (bool)me.CanPinMessages;

            if (canRestrictMembers && canDeleteMessages)
                text = CacheData.SysConfigs.Single(x => x.SysConfigId == "CommandCheckOkText").Value;

            text = text.Replace("{{has_ban_users}}", canRestrictMembers ? "{{true}}" : "{{false}}");
            text = text.Replace("{{has_delete_messages}}", canDeleteMessages ? "{{true}}" : "{{false}}");
            text = text.Replace("{{has_pin_messages}}", canPinMessages ? "{{true}}" : "{{false}}");

            text = Utils.Parsers.VariablesParser(text); // TODO select group's settings language

            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = text
                });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
