using System.Collections.Generic;

namespace Unifiedban.Terminal.Bot
{
    public class Commands
    {
        public const string MOTD = "MOTD";
        public const string HELP = "HELP";

        public static Dictionary<string, Command.ICommand> CommandsList;
        public static void Initialize()
        {
            CommandsList = new Dictionary<string, Command.ICommand>();
            CommandsList.Add(MOTD, new Command.Motd());
            CommandsList.Add(HELP, new Command.Help());
        }
    }
}
