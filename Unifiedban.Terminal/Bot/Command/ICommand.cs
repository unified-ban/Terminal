/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public interface ICommand
    {
        void Execute(Message message);
        void Execute(CallbackQuery callbackQuery);
    }
}
