/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Report : ICommand
    {
        static Dictionary<ChatId, DateTime> lastReport = new Dictionary<ChatId, DateTime>();

        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            DateTime last;
            lastReport.TryGetValue(message.Chat.Id, out last);
            if (last != null)
                if ((DateTime.UtcNow - last).TotalSeconds < 30)
                    return;

            if (!String.IsNullOrEmpty(message.Chat.Username))
            {

                Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "User *{0}:{1}* from group *{2}:[{3}]({4})* sent a /report\n\n" +
                            message.Text.Remove(0, 7),
                        message.From.Id,
                        message.From.Username,
                        message.Chat.Id,
                        message.Chat.Title,
                        "https://t.me/" + message.Chat.Username)
                );

                Manager.BotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        parseMode: ParseMode.Markdown,
                        text: String.Format(
                            "The Operators have been advised about your call.\n" +
                            "Wait of any of them to join your group.")
                    );
            }
            else
            {
                Manager.BotClient.SendTextMessageAsync(
                        chatId: CacheData.ControlChatId,
                        parseMode: ParseMode.Markdown,
                        text: String.Format(
                            "User *{0}:{1}* from group *{2}:{3}* sent a /report.\n" +
                            "The group is private. Check for him in our [support group](https://t.me/unifiedban_group).\n\n" +
                                message.Text.Remove(0, 7),
                            message.From.Id,
                            message.From.Username,
                            message.Chat.Id,
                            message.Chat.Title)
                    );

                Manager.BotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        parseMode: ParseMode.Markdown,
                        text: String.Format(
                            "The Operators have been advised of your report but your group is private.\n" +
                            "Please join our [support group](https://t.me/unifiedban_group).")
                    );
            }
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            throw new NotImplementedException();
        }
    }
}
