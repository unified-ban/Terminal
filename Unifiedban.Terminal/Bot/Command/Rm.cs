/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Rm : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            if (!Utils.BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Basic) &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return;
            }

            int amount = 1;

            string[] data = message.Text.Split(" ");
            if (data.Length >= 2)
            {
                bool isInt = int.TryParse(data[1], out amount);
                if (!isInt)
                {
                    Manager.BotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        parseMode: ParseMode.Markdown,
                        text: "The provided ammount is not a number.\n"
                            + "**Syntax:** /rm {number = 1}"
                        );
                    return;
                }
            }

            deleteLastMessages(message, amount);
        }

        public void Execute(CallbackQuery callbackQuery) { }

        private void deleteLastMessages(Message message, int amount = 1)
        {
            int startMessage = message.ReplyToMessage != null ? message.ReplyToMessage.MessageId : (message.MessageId - 1);
            int nextMessage = message.ReplyToMessage != null ? message.ReplyToMessage.MessageId : (message.MessageId - 1);

            while (nextMessage > startMessage - amount)
            {
                Manager.BotClient.DeleteMessageAsync(message.Chat.Id, nextMessage);
                nextMessage--;
            }
        }
    }
}
