using System;
using System.Collections.Generic;
using System.Linq;
using Unifiedban.Models;
using Unifiedban.Models.Group;

namespace Unifiedban.Terminal.Bot
{
    public class Functions
    {
        public bool RegisterGroup(long chatId, string title)
        {
            if (CacheData.Groups.ContainsKey(chatId))
                return false;

            BusinessLogic.Group.TelegramGroupLogic telegramGroupLogic =
                new BusinessLogic.Group.TelegramGroupLogic();
            TelegramGroup registered = telegramGroupLogic.Add(
                chatId, title, TelegramGroup.Status.Inactive,
                configuration: "",
                welcomeText: "",
                chatLanguage: "",
                settingsLanguage: "",
                reportChatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                rulesText: "",
                callerId: -2);
            if (registered == null)
                return false;

            CacheData.Groups.Add(chatId, registered);
            return true;
        }
    }
}
