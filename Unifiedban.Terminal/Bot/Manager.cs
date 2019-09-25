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
        public static string Username { get; private set; }
        public static ITelegramBotClient BotClient { get;  private set; }

        public static void Initialize(string apikey)
        {
            if(apikey == null)
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
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Warn,
                Message = $"Hello, World! I am user {me.Id} and my name is {me.FirstName}.",
                UserId = -1
            });

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
                    $"My *instance ID* is _{instanceId}_\n" +
                    $"and I was running on *machine* _{currentHostname}_" +
                    $"See you soon!"
            );
        }

        private static async void BotClient_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban.Bot.Manager.BotClient_OnCallbackQuery",
                Level = Models.SystemLog.Levels.Debug,
                Message = "CallbackQuery received",
                UserId = -1
            });
            return;
        }
        private static async void BotClient_OnMessage(object sender, MessageEventArgs e)
        {
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban.Bot.Manager.BotClient_OnMessage",
                Level = Models.SystemLog.Levels.Debug,
                Message = "Message received",
                UserId = -1
            });
            if (e.Message.Text != null)
            {
                if (e.Message.Text.StartsWith('/'))
                    await Task.Run(() => Command.Parser.Parse(e.Message));
            }
        }
    }
}
