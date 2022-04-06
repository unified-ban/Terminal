using System;
using System.Threading.Tasks;
using Quartz;
using Unifiedban.Models;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Jobs
{
    public class ChatToolsJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Data.Utils.Logging.AddLog(new SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "ChatToolsJob",
                Level = SystemLog.Levels.Debug,
                Message = "Executing ChatToolsJob",
                UserId = -1
            });

            ChatTools.CheckNightSchedule();
            ChatTools.RenewInviteLinks().Wait();

            return Task.CompletedTask;
        }
    }
}