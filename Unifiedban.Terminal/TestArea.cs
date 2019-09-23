using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal
{
    public class TestArea
    {
        public static void DoTest()
        {
            //Gimmeconf();
        }

        public static void Gimmeconf()
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
    }
}
