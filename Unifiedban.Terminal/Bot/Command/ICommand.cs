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
