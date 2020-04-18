/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Call : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            if (!Utils.BotTools.IsUserOperator(message.From.Id) &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                return;
            }

            if (!String.IsNullOrEmpty(message.Chat.Username))
            {
                Manager.BotClient.SendTextMessageAsync(
                        chatId: Convert.ToInt64(CacheData.SysConfigs
                                .Single(x => x.SysConfigId == "ControlChatId")
                                .Value),
                        parseMode: ParseMode.Markdown,
                        text: String.Format(
                            "User *{0}:{1}* from group *{2}:[{3}]({4})* is requesting an Operator.",
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
                            "The Operators have been advised about your call." +
                            "Wait of any of them to join your group.")
                    );
            }
            else
            {
                Manager.BotClient.SendTextMessageAsync(
                        chatId: Convert.ToInt64(CacheData.SysConfigs
                                .Single(x => x.SysConfigId == "ControlChatId")
                                .Value),
                        parseMode: ParseMode.Markdown,
                        text: String.Format(
                            "User *{0}:{1}* from group *{2}:{3}* is requesting an Operator.\n" +
                            "The group is private. Check for him in our [support group](https://t.me/unifiedban_group).",
                            message.From.Id,
                            message.From.Username,
                            message.Chat.Id,
                            message.Chat.Title)
                    );

                Manager.BotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        parseMode: ParseMode.Markdown,
                        text: String.Format(
                            "The Operators have been advised but your group is private.\n" +
                            "Please join our [support group](https://t.me/unifiedban_group).")
                    );
            }
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
