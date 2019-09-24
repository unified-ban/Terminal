using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Parser
    {
        public static void Parse(Message message)
        {
            string command = message.Text.Split(" ")[0].Remove(0, 1);
            if (command.Contains("@"))
            {
                if (!String.Equals(command.Split("@")[1],
                    Manager.Username, StringComparison.CurrentCultureIgnoreCase))
                    return;
                command = command.Split("@")[0];
            }

            if (!Commands.CommandsList.TryGetValue(command.ToUpper(), out ICommand parsedCommand))
            {
                Manager.BotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Invalid command!"
                );
                return;
            }

            parsedCommand.Execute(message);
        }
    }
}
