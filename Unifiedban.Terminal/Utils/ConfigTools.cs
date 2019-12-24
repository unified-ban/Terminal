using Hangfire;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Unifiedban.Terminal.Utils
{
    public class ConfigTools
    {
        public static void Initialize()
        {
            RecurringJob.AddOrUpdate("ConfigTools_SyncGroupsConfigToDatabase", () => SyncGroupsConfigToDatabase(), "0/30 0 * ? * *");
        }

        public static void SyncGroupsConfigToDatabase()
        {
            BusinessLogic.Group.TelegramGroupLogic telegramGroupLogic =
                new BusinessLogic.Group.TelegramGroupLogic();
            
            foreach (long group in CacheData.GroupConfigs.Keys)
                telegramGroupLogic.UpdateConfiguration(
                    group,
                    JsonConvert.SerializeObject(CacheData.GroupConfigs[group]),
                    -2);
        }
    }
}
