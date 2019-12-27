/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Parser
    {
        public static void Parse(Message message)
        {
            string command = message.Text.Split(" ")[0].Remove(0, 1);
            if (command.Contains("@"))
            {
                if (!String.Equals(command.Split("@")[1],
                    Manager.Username, StringComparison.CurrentCultureIgnoreCase))
                    return;
                command = command.Split("@")[0];
            }

            if (!Commands.CommandsList.TryGetValue(command.ToUpper(), out ICommand parsedCommand))
            {
#if DEBUG
                Manager.BotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: CacheData.GetTranslation("en", "error_invalid_command")
                );
#endif
                return;
            }

            parsedCommand.Execute(message);
        }

        public static void Parse(CallbackQuery callbackQuery)
        {
            string command = callbackQuery.Data.Split(" ")[0].Remove(0, 1);

            if (!Commands.CommandsList.TryGetValue(command.ToUpper(), out ICommand parsedCommand))
            {
#if DEBUG
                Manager.BotClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: CacheData.GetTranslation("en", "error_invalid_command")
                );
#endif
                return;
            }

            parsedCommand.Execute(callbackQuery);
        }
    }
}
