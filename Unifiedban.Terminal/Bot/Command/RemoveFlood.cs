using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class RemoveFlood : ICommand
    {
        public void Execute(Message message){ }

        public void Execute(CallbackQuery callbackQuery)
        {
            if (!Utils.BotTools.IsUserOperator(callbackQuery.Message.From.Id) &&
                !Utils.ChatTools.IsUserAdmin(callbackQuery.Message.Chat.Id, callbackQuery.Message.From.Id))
            {
                return;
            }

            bool parsed = int.TryParse(callbackQuery.Data.Split(" ")[1], out int userId);
            if (!parsed)
                return;

            Manager.BotClient.DeleteMessageAsync(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId);

            Manager.BotClient.RestrictChatMemberAsync(
                    callbackQuery.Message.Chat.Id,
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
    }
}
