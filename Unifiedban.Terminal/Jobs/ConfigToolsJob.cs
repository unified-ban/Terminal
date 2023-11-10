using System;
using System.Threading.Tasks;
using Quartz;
using Unifiedban.Models;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Jobs
{
    public class ConfigToolsJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Data.Utils.Logging.AddLog(new SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "ConfigToolsJob",
                Level = SystemLog.Levels.Debug,
                Message = "Executing ConfigToolsJob",
                UserId = -1
            });

            ConfigTools.SyncGroupsConfigToDatabase();
            ConfigTools.SyncWelcomeAndRulesText();
            ConfigTools.SyncGroupsToDatabase();
            ConfigTools.SyncNightScheduleToDatabase();

            return Task.CompletedTask;
        }
    }
}