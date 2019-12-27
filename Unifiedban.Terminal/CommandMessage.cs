/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal
{
    public class CommandMessage

    {
        public Message Message { get; set; }
        public string Command { get; set; }
        public string Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
