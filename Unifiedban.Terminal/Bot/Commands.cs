using System.Collections.Generic;

namespace Unifiedban.Terminal.Bot
{
    public class Commands
    {
        public const string MOTD = "MOTD";
        public const string HELP = "HELP";
        public const string START = "START";
        public const string RELOADCONF = "RELOADCONF";
        public const string ADDTRANSLATION = "ADDTRANSLATION";
        public const string GETTRANSLATION = "GETTRANSLATION";
        public const string RELOADTRANSLATIONS = "RELOADTRANSLATIONS";
        public const string RELOADTRANSLATION = "RELOADTRANSLATION";

        public static Dictionary<string, Command.ICommand> CommandsList;
        public static void Initialize()
        {
            CommandsList = new Dictionary<string, Command.ICommand>();
            CommandsList.Add(MOTD, new Command.Motd());
            CommandsList.Add(HELP, new Command.Help());
            CommandsList.Add(START, new Command.Start());
            CommandsList.Add(RELOADCONF, new Command.ReloadConf());
            CommandsList.Add(ADDTRANSLATION, new Command.AddTranslation());
            CommandsList.Add(GETTRANSLATION, new Command.GetTranslation());
            CommandsList.Add(RELOADTRANSLATIONS, new Command.GetTranslation());
            CommandsList.Add(RELOADTRANSLATION, new Command.GetTranslation());
        }
    }
}
