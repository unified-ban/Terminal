/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                if (MessageQueueManager.AddChatIfNotPresent(message.Chat.Id))
                {
                    Models.SysConfig startMessage = CacheData.SysConfigs
                            .SingleOrDefault(x => x.SysConfigId == "StartMessage");
                    if (startMessage == null)
                    {
                        Manager.BotClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"Your chat {message.Chat.Title} has been added successfully!"
                        );
                        return;
                    }

                    Manager.BotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        text: Utils.Parsers.VariablesParser(startMessage.Value, message)
                    );
                    return;
                }

                Manager.BotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Error adding chat {message.Chat.Title}! Please contact our support"
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
