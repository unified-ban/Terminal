using System;
using System.Net;
using System.Threading.Tasks;
using Quartz;
using Unifiedban.Models;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Jobs
{
    public class LogsJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Data.Utils.Logging.AddLog(new SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "LogsJob",
                Level = SystemLog.Levels.Debug,
                Message = "Executing LogsJob",
                UserId = -1
            });

            LogTools.SyncSystemLog();
            LogTools.SyncActionLog();
            LogTools.SyncTrustFactorLog();
            LogTools.SyncOperationLog();
            LogTools.SyncSupportSessionLog();

            return Task.CompletedTask;
        }
    }
}