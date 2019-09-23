﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Motd : ICommand
    {
        public Task Execute(Message message)
        {
            return Manager.BotClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: CacheData.Configuration["motd"]
            );
        }
    }
}
