/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot
{
    public class Manager
    {
        static string APIKEY;
        static string instanceId = "";
        static string currentHostname = "";
        public static int MyId = 0;
        public static string Username { get; private set; }
        public static ITelegramBotClient BotClient { get;  private set; }

        public static void Initialize(string apikey)
        {
            if (CacheData.FatalError)
                return;

            if (apikey == null)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban Terminal Startup",
                    Level = Models.SystemLog.Levels.Fatal,
                    Message = "API KEY cannot be null!",
                    UserId = -1
                });
                CacheData.FatalError = true;
                return;
            }

            APIKEY = apikey;
            instanceId = Guid.NewGuid().ToString();
            currentHostname = System.Net.Dns.GetHostName();
            Commands.Initialize();

            BotClient = new TelegramBotClient(APIKEY);
            var me = BotClient.GetMeAsync().Result;
            Username = me.Username;
            MyId = me.Id;
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Warn,
                Message = $"Hello, World! I am user {me.Id} and my name is {me.FirstName}.",
                UserId = -1
            });

            MessageQueueManager.AddChatIfNotPresent(CacheData.ControlChatId);

            BotClient.OnMessage += BotClient_OnMessage;
            BotClient.OnCallbackQuery += BotClient_OnCallbackQuery;
            BotClient.StartReceiving();

            BotClient.SendTextMessageAsync(
                chatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                parseMode: ParseMode.Markdown,
                text: $"I'm here, Master.\n" +
                    $"My *instance ID* is _{instanceId}_ " +
                    $"and I'm running on *machine* _{currentHostname}_"
            );
            Console.Title = $"Unifiedban - Username: {me.Username} - Instance ID: {instanceId}";


            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Bot Client initialized",
                UserId = -2
            });
        }

        public static void Dispose()
        {
            BotClient.StopReceiving();
            BotClient.SendTextMessageAsync(
                chatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                parseMode: ParseMode.Markdown,
                text: $"I left, Master.\n" +
                    $"My *instance ID* is _{instanceId}_ " +
                    $"and I was running on *machine* _{currentHostname}_\n" +
                    $"See you soon!"
            );
        }

        private static async void BotClient_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            if (e.CallbackQuery.Message.Date < DateTime.Now.AddDays(-1))

                return;
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban.Bot.Manager.BotClient_OnCallbackQuery",
                Level = Models.SystemLog.Levels.Debug,
                Message = "CallbackQuery received",
                UserId = -1
            });

            if (!String.IsNullOrEmpty(e.CallbackQuery.Data))
            {
                if (e.CallbackQuery.Data.StartsWith('/'))
                    await Task.Run(() => Command.Parser.Parse(e.CallbackQuery));
            }

            return;
        }
        private static async void BotClient_OnMessage(object sender, MessageEventArgs e)
        {
            if (e == null)
                return;

            if (e.Message.Date < DateTime.Now.AddDays(-1))
                return;

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban.Bot.Manager.BotClient_OnMessage",
                Level = Models.SystemLog.Levels.Debug,
                Message = "Message received",
                UserId = -1
            });
            await Task.Run(() => CacheData.IncrementHandledMessages());

            if (e.Message.MigrateToChatId != 0)
            {
                Functions.MigrateToChatId(e.Message);
            }

            if (!String.IsNullOrEmpty(e.Message.Text))
            {
                if (e.Message.Text.StartsWith('/'))
                {
                    await Task.Run(() => Command.Parser.Parse(e.Message));
                    return;
                }

                if (e.Message.ReplyToMessage != null)
                {
                    if (e.Message.ReplyToMessage.From.Id == MyId)
                    {
                        CommandQueueManager.ReplyMessage(e.Message);
                        return;
                    }
                }

                Controls.Manager.DoCheck(e.Message);
            }

            if (e.Message.NewChatMembers != null)
                Functions.UserJoinedAction(e.Message);                

            if (!String.IsNullOrEmpty(e.Message.MediaGroupId) ||
                e.Message.Photo != null ||
                e.Message.Document != null)
                Controls.Manager.DoMediaCheck(e.Message);
        }
    }
}
