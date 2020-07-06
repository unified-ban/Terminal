/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Unifiedban.Models;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Parser
    {
        public static async Task<bool> Parse(Message message)
        {
            string command = message.Text.Split(" ")[0].Remove(0, 1);
            if (command.Contains("@"))
            {
                if (!String.Equals(command.Split("@")[1],
                    Manager.Username, StringComparison.CurrentCultureIgnoreCase))
                    return false;
                command = command.Split("@")[0];
            }

            if (!Commands.CommandsList.TryGetValue(command.ToUpper(), out ICommand parsedCommand))
            {
#if DEBUG
                if(CacheData.AnswerInvalidCommand)
                    await Manager.BotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: CacheData.GetTranslation("en", "error_invalid_command")
                    );
#endif
                return false;
            }
            
            await Task.Run(() => Utils.LogTools.AddOperationLog(new OperationLog()
            {
                UtcDate = DateTime.UtcNow,
                GroupId = CacheData.Groups[message.Chat.Id].GroupId,
                TelegramUserId = message.From.Id,
                Action = command,
                Parameters = command.Contains(" ") ? message.Text.Substring(command.Length + 1) : ""
            }));
            await Task.Run(() => parsedCommand.Execute(message));
            return true;
        }

        public static async Task<bool> Parse(CallbackQuery callbackQuery)
        {
            string command = callbackQuery.Data.Split(" ")[0].Remove(0, 1);

            if (!Commands.CommandsList.TryGetValue(command.ToUpper(), out ICommand parsedCommand))
            {
#if DEBUG
                await Manager.BotClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: CacheData.GetTranslation("en", "error_invalid_command")
                );
#endif
                return false;
            }

            await Task.Run(() => parsedCommand.Execute(callbackQuery));
            return true;
        }
    }
}
