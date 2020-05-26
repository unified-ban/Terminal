/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Hangfire;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
            RecurringJob.AddOrUpdate("LogTools_SyncSystemLog", () => SyncSystemLog(), "0/30 * * ? * *");
            RecurringJob.AddOrUpdate("LogTools_SyncActionLog", () => SyncActionLog(), "0/30 * * ? * *");
            RecurringJob.AddOrUpdate("LogTools_SyncTrustFactorLog", () => SyncTrustFactorLog(), "0/30 * * ? * *");
            RecurringJob.AddOrUpdate("LogTools_SyncOperatorLog", () => SyncOperatorLog(), "0/30 * * ? * *");
            RecurringJob.AddOrUpdate("LogTools_SyncSupportSessionLog", () => SyncSupportSessionLog(), "0/30 * * ? * *");
        }

        public static void Dispose()
        {
            SyncSystemLog();
            SyncActionLog();
            SyncTrustFactorLog();
            SyncOperatorLog();
            SyncSupportSessionLog();
        }
        
        public static void SyncSystemLog()
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
        public static void SyncActionLog()
        {
            List<ActionLog> logsToSync = new List<ActionLog>();
            lock (systemLogLock)
            {
                logsToSync = new List<ActionLog>(actionLogs);
                systemLogs.Clear();
            }
            
            if (logsToSync.Count() != 0)
            {
                all.Add(logsToSync, -2);
            }
        }
        public static void SyncTrustFactorLog()
        {
            List<TrustFactorLog> logsToSync = new List<TrustFactorLog>();
            lock (systemLogLock)
            {
                logsToSync = new List<TrustFactorLog>(trustFactorLogs);
                systemLogs.Clear();
            }
            
            if (logsToSync.Count() != 0)
            {
                tfll.Add(logsToSync, -2);
            }
        }
        public static void SyncOperatorLog()
        {
            List<OperationLog> logsToSync = new List<OperationLog>();
            lock (systemLogLock)
            {
                logsToSync = new List<OperationLog>(operationLogs);
                systemLogs.Clear();
            }
            
            if (logsToSync.Count() != 0)
            {
                oll.Add(logsToSync, -2);
            }
        }
        public static void SyncSupportSessionLog()
        {
            List<SupportSessionLog> logsToSync = new List<SupportSessionLog>();
            lock (systemLogLock)
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
            lock (systemLogLock)
            {
                actionLogs.Add(log);
            }
        }
        public static void AddTrustFactorLog(TrustFactorLog log)
        {
            lock (systemLogLock)
            {
                trustFactorLogs.Add(log);
            }
        }
        public static void AddOperatorLog(OperationLog log)
        {
            lock (systemLogLock)
            {
                operationLogs.Add(log);
            }
        }
        public static void AddSupportSessionLog(SupportSessionLog log)
        {
            lock (systemLogLock)
            {
                supportSessionLogs.Add(log);
            }
        }
    }
}