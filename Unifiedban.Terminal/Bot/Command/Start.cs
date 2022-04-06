/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Start : ICommand
    {
        public void Execute(Message message)
        {
            if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private
                || message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Channel) {

                Manager.BotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"This bot works only in groups. Add it to a group and will auto-start."
                    );
                return;
            }

            if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Group
                || message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Supergroup)
            {
                return;
            }

            Manager.BotClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Error: chat type not recognized. Please contact our support."
            );
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            return;
        }
    }
}
