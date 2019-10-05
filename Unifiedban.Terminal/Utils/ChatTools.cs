using System;
using System.Collections.Generic;
using System.Text;

namespace Unifiedban.Terminal.Utils
{
    public class ChatTools
    {
        public static bool IsUserAdmin(long chatId, long userId)
        {
            var administrators = Bot.Manager.BotClient.GetChatAdministratorsAsync(chatId).Result;
            foreach(Telegram.Bot.Types.ChatMember member in administrators)
            {
                if (member.User.Id == userId)
                    return true;
            }
            return false;
        }
    }
}
