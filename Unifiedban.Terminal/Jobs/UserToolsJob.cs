using System;
using System.Threading.Tasks;
using Quartz;
using Unifiedban.Models;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Jobs
{
    public class UserToolsJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Data.Utils.Logging.AddLog(new SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "UserToolsJob",
                Level = SystemLog.Levels.Debug,
                Message = "Executing UserToolsJob",
                UserId = -1
            });

            UserTools.SyncTrustFactor();
            UserTools.SyncBlacklist();

            return Task.CompletedTask;
        }
    }
}