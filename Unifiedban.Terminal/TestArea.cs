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
            //Gimmeconf();
            //RegisterOperators();
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
