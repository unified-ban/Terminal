/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;

namespace Unifiedban.Terminal.Bot
{
    public class Commands
    {
        public const string MOTD = "MOTD";
        public const string HELP = "HELP";
        public const string H = "H";
        public const string START = "START";
        public const string RELOADCONF = "RELOADCONF";
        public const string ADDTRANSLATION = "ADDTRANSLATION";
        public const string GETTRANSLATION = "GETTRANSLATION";
        public const string RELOADTRANSLATIONS = "RELOADTRANSLATIONS";
        public const string RELOADTRANSLATION = "RELOADTRANSLATION";
        public const string ECHO = "ECHO";
        public const string CHECK = "CHECK";
        public const string C = "C";
        public const string PIN = "PIN";
        public const string P = "P";
        public const string ID = "ID";
        public const string GET = "GET";
        public const string GETME = "GETME";
        public const string A = "A";
        public const string ANNOUNCE = "ANNOUNCE";
        public const string LEAVE = "LEAVE";
        public const string CAPTCHA = "CAPTCHA";
        public const string RULES = "RULES";
        public const string R = "R";
        public const string STATUS = "STATUS";
        public const string CONFIG = "CONFIG";
        public const string SETWELCOME = "SETWELCOME";
        public const string SETRULES = "SETRULES";
        public const string REMOVEFLOOD = "REMOVEFLOOD";
        public const string KICK = "KICK";
        public const string MUTE = "MUTE";
        public const string UNMUTE = "UNMUTE";
        public const string GATE = "GATE";
        public const string RM = "RM";
        public const string ENABLE = "ENABLE";
        public const string DISABLE = "DISABLE";
        public const string SETNOTE = "SETNOTE";
        public const string ADDNOTE = "ADDNOTE";
        public const string REMOVENOTE = "REMOVENOTE";
        public const string DELETENOTE = "DELETENOTE";
        public const string BAN = "BAN";
        public const string UNBAN = "UNBAN";
        public const string ADDSAFE = "ADDSAFE";
        public const string REMSAFE = "REMSAFE";
        public const string ADDWELCOMEBUTTON = "ADDWELCOMEBUTTON";
        public const string AWB = "AWB";
        public const string WELCOMEBUTTONS = "WELCOMEBUTTONS";
        public const string WBL = "WBL";
        public const string REMOVEWELCOMEBUTTON = "REMOVEWELCOMEBUTTON";
        public const string RWB = "RWB";
        public const string BAD = "BAD";
        public const string UNBAD = "UNBAD";
        public const string CALL = "CALL";

        public const string TEST1 = "TEST1";

        public static Dictionary<string, Command.ICommand> CommandsList;
        public static void Initialize()
        {
            CommandsList = new Dictionary<string, Command.ICommand>();
            CommandsList.Add(MOTD, new Command.Motd());
            CommandsList.Add(HELP, new Command.Help());
            CommandsList.Add(H, new Command.Help());
            CommandsList.Add(START, new Command.Start());

            CommandsList.Add(RELOADCONF, new Command.ReloadConf());

            CommandsList.Add(ADDTRANSLATION, new Command.AddTranslation());
            CommandsList.Add(GETTRANSLATION, new Command.GetTranslation());
            CommandsList.Add(RELOADTRANSLATIONS, new Command.ReloadTranslations());
            CommandsList.Add(RELOADTRANSLATION, new Command.ReloadTranslations());

            CommandsList.Add(ECHO, new Command.Echo());

            CommandsList.Add(ENABLE, new Command.Enable());
            CommandsList.Add(DISABLE, new Command.Disable());

            CommandsList.Add(CHECK, new Command.Check());
            CommandsList.Add(C, new Command.Check());

            CommandsList.Add(PIN, new Command.Pin());
            CommandsList.Add(P, new Command.Pin());

            CommandsList.Add(ID, new Command.Id());

            CommandsList.Add(GET, new Command.Get());
            CommandsList.Add(GETME, new Command.Get());

            CommandsList.Add(A, new Command.Announce());
            CommandsList.Add(ANNOUNCE, new Command.Announce());

            CommandsList.Add(LEAVE, new Command.Leave());

            CommandsList.Add(CAPTCHA, new Command.Captcha());

            CommandsList.Add(RULES, new Command.Rules());
            CommandsList.Add(R, new Command.Rules());

            CommandsList.Add(STATUS, new Command.Status());

            CommandsList.Add(CONFIG, new Command.Config());
            CommandsList.Add(SETWELCOME, new Command.SetWelcome());
            CommandsList.Add(SETRULES, new Command.SetRules());


            CommandsList.Add(REMOVEFLOOD, new Command.RemoveFlood());
            CommandsList.Add(KICK, new Command.Kick());
            CommandsList.Add(MUTE, new Command.Mute());
            CommandsList.Add(UNMUTE, new Command.Unmute());
            CommandsList.Add(GATE, new Command.Gate());
            CommandsList.Add(RM, new Command.Rm());
            CommandsList.Add(SETNOTE, new Command.AddNote());
            CommandsList.Add(ADDNOTE, new Command.AddNote());
            CommandsList.Add(REMOVENOTE, new Command.RemoveNote());
            CommandsList.Add(DELETENOTE, new Command.RemoveNote());
            CommandsList.Add(BAN, new Command.Ban());
            CommandsList.Add(UNBAN, new Command.Unban());
            CommandsList.Add(ADDSAFE, new Command.AddSafeGroup());
            CommandsList.Add(REMSAFE, new Command.RemoveSafeGroup());
            CommandsList.Add(ADDWELCOMEBUTTON, new Command.AddWelcomeButton());
            CommandsList.Add(AWB, new Command.AddWelcomeButton());
            CommandsList.Add(WELCOMEBUTTONS, new Command.WelcomeButtonsList());
            CommandsList.Add(WBL, new Command.WelcomeButtonsList());
            CommandsList.Add(REMOVEWELCOMEBUTTON, new Command.RemoveWelcomeButton());
            CommandsList.Add(RWB, new Command.RemoveWelcomeButton());
            CommandsList.Add(BAD, new Command.AddBadWord());
            CommandsList.Add(UNBAD, new Command.RemoveBadWord());
            CommandsList.Add(CALL, new Command.Call());

#if DEBUG
            CommandsList.Add(TEST1, new Command.TestCommand());
#endif
        }
    }
}
