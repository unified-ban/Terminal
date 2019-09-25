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
