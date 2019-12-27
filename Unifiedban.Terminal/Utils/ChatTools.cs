/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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
