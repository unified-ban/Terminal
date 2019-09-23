using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Unifiedban.Terminal.Bot.Command
{
    public interface ICommand
    {
        Task Execute();
        Task Execute(string parameter);
    }
}
