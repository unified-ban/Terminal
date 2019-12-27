/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Motd : ICommand
    {
        public void Execute(Message message)
        {
            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    Text = CacheData.SysConfigs
                        .Single(x => x.SysConfigId == "motd")
                        .Value
                });
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            return;
        }
    }
}
