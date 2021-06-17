/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Diagnostics;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Threading.Tasks;
using System.Linq;
using Hangfire;

namespace Unifiedban.Terminal.Bot.Command
{
    public class TSSRenewLinks : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            if (CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == message.From.Id
                && x.Level >= Models.Operator.Levels.Advanced) == null)
            {
                return;
            }

            RecurringJob.Trigger("ChatTools_RenewInviteLinks");
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            return;
        }
    }
}
