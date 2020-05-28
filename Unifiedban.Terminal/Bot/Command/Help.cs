/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Help : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    Text = Utils.Parsers.VariablesParser(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "HelpMenu")
                            .Value)
                });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
