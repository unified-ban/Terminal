/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal
{
    public class TestArea
    {
#if DEBUG
        public static void DoTest()
        {
            if (CacheData.FatalError)
                return;

            //Gimmeconf();
            //RegisterOperators();
            // var user = Bot.Manager.BotClient.GetChatMemberAsync(-1001125553456, 560445026).Result;
            // Console.WriteLine(user.User.Username);
        }

        static void Gimmeconf()
        {
            InlineKeyboardMarkup conf = new InlineKeyboardMarkup(
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithUrl(
                        "Project website",
                        "https://unifiedban.solutions"
                        )
                }
            );

            string confString = JsonConvert.SerializeObject(conf);
            Console.WriteLine(confString);
        }

        static void RegisterOperators()
        {
            BusinessLogic.OperatorLogic operatorLogic = new BusinessLogic.OperatorLogic();
            //operatorLogic.Add(799698579, Models.Operator.Levels.Super, -1);
            //operatorLogic.Add(339380551, Models.Operator.Levels.Super, -1);
        }
#endif
    }
}
