/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Hangfire;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Quartz;
using Unifiedban.BusinessLogic;
using Unifiedban.Models;

namespace Unifiedban.Terminal.Utils
{
    public class LogTools
    {
        static SystemLogLogic sll = new SystemLogLogic();
        static List<SystemLog> systemLogs = new List<SystemLog>();
        static object systemLogLock = new object();
        
        static ActionLogLogic all = new ActionLogLogic();
        static List<ActionLog> actionLogs = new List<ActionLog>();
        static object actionLogLock = new object();
        
        static TrustFactorLogLogic tfll = new TrustFactorLogLogic();
        static List<TrustFactorLog> trustFactorLogs = new List<TrustFactorLog>();
        static object trustFactorLogLock = new object();
        
        static OperationLogLogic oll = new OperationLogLogic();
        static List<OperationLog> operationLogs = new List<OperationLog>();
        static object operatorLogLock = new object();
        
        static SupportSessionLogLogic ssl = new SupportSessionLogLogic();
        static List<SupportSessionLog> supportSessionLogs = new List<SupportSessionLog>();
        static object supportSessionLogLock = new object();

        public static void Initialize()
        {
            // RecurringJob.AddOrUpdate("LogTools_SyncSystemLog", () => SyncSystemLog(), "0/30 * * ? * *");
            // RecurringJob.AddOrUpdate("LogTools_SyncActionLog", () => SyncActionLog(), "0/30 * * ? * *");
            // RecurringJob.AddOrUpdate("LogTools_SyncTrustFactorLog", () => SyncTrustFactorLog(), "0/30 * * ? * *");
            // RecurringJob.AddOrUpdate("LogTools_SyncOperationLog", () => SyncOperationLog(), "0/30 * * ? * *");
            // RecurringJob.AddOrUpdate("LogTools_SyncSupportSessionLog", () => SyncSupportSessionLog(), "0/30 * * ? * *");
            
            var syncLogJob = JobBuilder.Create<Jobs.LogsJob>()
                .WithIdentity("syncLogJob", "logs")
                .Build();
            var syncLogJobTrigger = TriggerBuilder.Create()
                .WithIdentity("syncLogJobTrigger", "logs")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(30)
                    .RepeatForever())
                .Build();
            Program.Scheduler?.ScheduleJob(syncLogJob, syncLogJobTrigger).Wait();
        }

        public static void Dispose()
        {
            SyncSystemLog();
            SyncActionLog();
            SyncTrustFactorLog();
            SyncOperationLog();
            SyncSupportSessionLog();
        }
        
        internal static void SyncSystemLog()
        {
            List<SystemLog> logsToSync = new List<SystemLog>();
            lock (systemLogLock)
            {
                logsToSync = new List<SystemLog>(systemLogs);
                systemLogs.Clear();
            }

            if (logsToSync.Count() != 0)
            {
                sll.Add(logsToSync, -2);
            }
        }
        internal static void SyncActionLog()
        {
            List<ActionLog> logsToSync = new List<ActionLog>();
            lock (actionLogLock)
            {
                logsToSync = new List<ActionLog>(actionLogs);
                actionLogs.Clear();
            }
            
            if (logsToSync.Count() != 0)
            {
                all.Add(logsToSync, -2);
            }
        }
        internal static void SyncTrustFactorLog()
        {
            List<TrustFactorLog> logsToSync = new List<TrustFactorLog>();
            lock (trustFactorLogs)
            {
                logsToSync = new List<TrustFactorLog>(trustFactorLogs);
                trustFactorLogs.Clear();
            }
            
            if (logsToSync.Count() != 0)
            {
                tfll.Add(logsToSync, -2);
            }
        }
        internal static void SyncOperationLog()
        {
            List<OperationLog> logsToSync = new List<OperationLog>();
            lock (operatorLogLock)
            {
                logsToSync = new List<OperationLog>(operationLogs);
                operationLogs.Clear();
            }
            
            if (logsToSync.Count() != 0)
            {
                foreach (var log in logsToSync)
                {
                    oll.Add(log, -2);
                }
            }
        }
        internal static void SyncSupportSessionLog()
        {
            List<SupportSessionLog> logsToSync = new List<SupportSessionLog>();
            lock (supportSessionLogLock)
            {
                logsToSync = new List<SupportSessionLog>(supportSessionLogs);
                systemLogs.Clear();
            }
            
            if (logsToSync.Count() != 0)
            {
                ssl.Add(logsToSync, -2);
            }
        }

        public static void AddSystemLog(SystemLog log)
        {
            lock (systemLogLock)
            {
                systemLogs.Add(log);
            }
        }
        public static void AddActionLog(ActionLog log)
        {
            lock (actionLogLock)
            {
                actionLogs.Add(log);
            }
        }
        public static void AddTrustFactorLog(TrustFactorLog log)
        {
            lock (trustFactorLogLock)
            {
                trustFactorLogs.Add(log);
            }
        }
        public static void AddOperationLog(OperationLog log)
        {
            lock (operationLogs)
            {
                operationLogs.Add(log);
            }
        }
        public static void AddSupportSessionLog(SupportSessionLog log)
        {
            lock (supportSessionLogLock)
            {
                supportSessionLogs.Add(log);
            }
        }
    }
}