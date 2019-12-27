/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Threading.Tasks;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Status : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            Process proc = Process.GetCurrentProcess();
            float usedRam = (proc.WorkingSet64 / 1024f) / 1024f;
            string SQLStatus = "*Offline* ⚠";
            if(SQLOnline())
                SQLStatus = "Online";
            string env = "production";
#if DEBUG
            env = "🛠 *BETA* 🛠";
#endif

            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = $"Instance *started*: {proc.StartTime}\n" +
                        $"Used *RAM* is {Math.Round(usedRam, 2)}MB\n" +
                        $"Used *CPU* is {Math.Round(GetCpuUsageForProcess().Result, 2)}%\n" +
                        $"*Database* status is {SQLStatus}\n" +
                        $"*Environment* is {env}\n" +
                        $"*Messages since start:* {CacheData.HandledMessages}"
                });
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            if (!Utils.BotTools.IsUserOperator(callbackQuery.From.Id, Models.Operator.Levels.Super))
            {
                return;
            }
        }

        private async Task<double> GetCpuUsageForProcess()
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            await Task.Delay(500);
            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return cpuUsageTotal * 100;
        }

        private bool SQLOnline()
        {
            BusinessLogic.SysConfigLogic sysConfigLogic = new BusinessLogic.SysConfigLogic();
            var conf = sysConfigLogic.GetById("motd");
            return conf != null ? true : false;
        }
    }
}
