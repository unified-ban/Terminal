using System;
using System.Net;
using System.Threading.Tasks;
using Quartz;
using Unifiedban.Models;

namespace Unifiedban.Terminal.Jobs
{
    public class UptimeJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            if (CacheData.Configuration?["UptimeMonitor:URL"] is null) return Task.CompletedTask;
            Data.Utils.Logging.AddLog(new SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "UptimeJob",
                Level = SystemLog.Levels.Debug,
                Message = "Sending Uptime Hearthbeat",
                UserId = -1
            });
            
            WebClient wc = new();
            try
            {
                wc.DownloadString(CacheData.Configuration["UptimeMonitor:URL"]);
            }
            catch (Exception ex)
            {
                Data.Utils.Logging.AddLog(new SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "UptimeJob",
                    Level = SystemLog.Levels.Warn,
                    Message = $"Can't send heartbeat: \n{ex.Message}",
                    UserId = -1
                });
            }

            return Task.CompletedTask;
        }
    }
}