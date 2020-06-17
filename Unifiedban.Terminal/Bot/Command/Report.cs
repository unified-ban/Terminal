/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Unifiedban.Models;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Report : ICommand
    {
        static Dictionary<ChatId, DateTime> lastReport = new Dictionary<ChatId, DateTime>();

        public void Execute(Message message)
        {
            DateTime last;
            lastReport.TryGetValue(message.Chat.Id, out last);
            if (last != null)
                if ((DateTime.UtcNow - last).TotalSeconds < 30)
                    return;

            string author = message.From.Username == null
                ? message.From.FirstName + " " + message.From.LastName
                : "@" + message.From.Username;
            string logMessage = String.Format(
                "*[Report]*\n" +
                "A user has reported a message\n" +
                "⚠ do not open links you don't know ⚠\n" +
                "\nChat: `{0}`" +
                "\nAuthor: `{1}`" +
                "\nUserId: `{2}`" +
                "\nOriginal message link: https://t.me/c/{4}/{3}\n" +
                "\n\n*hash_code:* #UB{4}-{5}",
                message.Chat.Title,
                author,
                message.From.Id,
                message.MessageId,
                message.Chat.Id.ToString().Replace("-", ""),
                Guid.NewGuid());
            
            
            MessageQueueManager.EnqueueLog(new ChatMessage()
            {
                ParseMode = ParseMode.Markdown,
                Text = logMessage
            });
            
            if (!String.IsNullOrEmpty(message.Chat.Username))
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = "The Operators have been advised about your call.\n" +
                               "Wait of any of them to join your group."
                    });
            }
            else
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = "The Operators have been advised of your report but your group is private.\n" +
                               "Please join our [support group](https://t.me/unifiedban_group)."
                    });
            }
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            throw new NotImplementedException();
        }
    }
}
