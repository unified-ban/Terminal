/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot
{
    public class Manager
    {
        static string APIKEY;
        static string instanceId = "";
        static string currentHostname = "";
        private static ushort pastHoursToSkip = 12;
        private static bool throwPendingUpdates;
        public static long MyId = 0;
        public static string Username { get; private set; }
        public static TelegramBotClient BotClient { get;  private set; }

        public static CancellationTokenSource Cts = new();

        private static QueuedUpdateReceiver _updateReceiver;

        public static void Initialize(string apikey)
        {
            if (CacheData.FatalError)
                return;

            if (string.IsNullOrEmpty(apikey))
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban Terminal Startup",
                    Level = Models.SystemLog.Levels.Fatal,
                    Message = "API KEY must be set!",
                    UserId = -1
                });
                CacheData.FatalError = true;
                return;
            }

            ushort.TryParse(CacheData.Configuration["PastHoursToSkip"], out pastHoursToSkip);
            bool.TryParse(CacheData.Configuration["ThrowPendingUpdates"], out throwPendingUpdates);

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

        public static async void StartReceiving()
        {
            await BotClient.SendTextMessageAsync(
                chatId: CacheData.ControlChatId,
                parseMode: ParseMode.Markdown,
                text: $"I'm here, dude.\n" +
                    $"My *instance ID* is _{instanceId}_ " +
                    $"and I'm running on *machine* _{currentHostname}_\n" +
#if DEBUG
                    $"- unified/ban BETA"
#else
                    $"- unified/ban"
#endif
            );

            var infoUrl = $"https://api.telegram.org/bot{APIKEY}/getWebhookInfo";
            var infoClient = new HttpClient();
            var infoRes = await infoClient.GetAsync(infoUrl);
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "StartReceiving",
                Level = Models.SystemLog.Levels.Warn,
                Message = infoRes.Content.ReadAsStringAsync().Result,
                UserId = -2
            });
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "StartReceiving",
                Level = Models.SystemLog.Levels.Warn,
                Message = $"Skipping pending updates older than: {pastHoursToSkip} hour(s)",
                UserId = -2
            });
            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "StartReceiving",
                Level = Models.SystemLog.Levels.Warn,
                Message = $"Throwing pending updates: {throwPendingUpdates}",
                UserId = -2
            });
            
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
                ThrowPendingUpdates = throwPendingUpdates
            };
            
            BotClient.StartReceiving(
                updateHandler: UpdateHandler,
                pollingErrorHandler: PollingErrorHandler,
                receiverOptions: receiverOptions,
                cancellationToken: Cts.Token);

            /*_updateReceiver = new QueuedUpdateReceiver(BotClient, receiverOptions);
            try
            {
                await foreach (Update update in _updateReceiver.WithCancellation(Cts.Token))
                {
                    if (update.Message is not null)
                        HandleUpdateAsync(update.Message);
                    if (update.CallbackQuery is not null)
                        HandleCallbackQuery(update.CallbackQuery);
                }
            }
            catch (OperationCanceledException ex)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Message Receiver",
                    Level = Models.SystemLog.Levels.Fatal,
                    Message = $"CTS Req: {Cts.IsCancellationRequested} - {ex.Message}",
                    UserId = -2
                });
            }
            catch (Exception ex)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Message Receiver",
                    Level = Models.SystemLog.Levels.Fatal,
                    Message = ex.Message,
                    UserId = -2
                });
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Message Receiver",
                    Level = Models.SystemLog.Levels.Fatal,
                    Message = ex.InnerException?.Message,
                    UserId = -2
                });
            }*/
                
            // Program.DisposeAll();
        }

        private static Task PollingErrorHandler(ITelegramBotClient botClient, Exception ex, CancellationToken arg3)
        {
            var errMsg = ex switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => ex.ToString()
            };

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "PollingErrorHandler",
                Level = Models.SystemLog.Levels.Fatal,
                Message = $"Polling ERROR: {errMsg}",
                UserId = -2
            });

            if(!Cts.IsCancellationRequested)
                Restart(ex.Message);
            
            return Task.CompletedTask;
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken arg3)
        {
            if (update.Message is not null)
            {
                await HandleUpdateAsync(update.Message);
            }

            if (update.CallbackQuery is not null)
            {
                await HandleCallbackQuery(update.CallbackQuery);
            }
        }

        public static void Dispose()
        {
            Cts.Cancel();
            BotClient.SendTextMessageAsync(
                chatId: CacheData.ControlChatId,
                parseMode: ParseMode.Markdown,
                text: $"I left, dude.\n" +
                    $"My *instance ID* is _{instanceId}_ " +
                    $"and I was running on *machine* _{currentHostname}_\n" +
                    $"See you soon!\n" +
#if DEBUG
                    $"- unified/ban BETA"
#else
                    $"- unified/ban"
#endif
            );
        }

        private static void Restart(string reason)
        {
            Cts.Cancel();
            ushort wait = 45;

            if (reason.Contains("(Too Many Requests: retry after"))
            {
                var required = reason.Split("(Too Many Requests: retry after ")[1];
                required = required.Split(")")[0];
                ushort.TryParse(required, out wait);
            }
            Thread.Sleep(1000 * wait);
            
            BotClient.SendTextMessageAsync(
                chatId: -1001125553456,
                parseMode: ParseMode.Markdown,
                disableNotification: true,
                text: $"Restarting instance {instanceId}\n" +
                      $"Reason: {reason}\n\n" +
#if DEBUG
                    $"- unified/ban BETA"
#else
                      $"- unified/ban"
#endif
            );
            
            Cts.Dispose();
            
            Thread.Sleep(1000 * 10);
            Cts = new();
            StartReceiving();
        }
        
        private static async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message!.Date < DateTime.Now.AddHours(-pastHoursToSkip))
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Bot.Manager.BotClient_OnCallbackQuery",
                    Level = Models.SystemLog.Levels.Debug,
                    Message = $"Skipping callback older than {pastHoursToSkip} hour(s)",
                    UserId = -1
                });
                return;
            }
            
            await Task.Run(() => CacheData.IncrementHandledMessages());

            if(CacheData.Groups[callbackQuery.Message.Chat.Id].State != 
                Models.Group.TelegramGroup.Status.Active) return;

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban.Bot.Manager.BotClient_OnCallbackQuery",
                Level = Models.SystemLog.Levels.Debug,
                Message = "CallbackQuery received",
                UserId = -1
            });

            if (!string.IsNullOrEmpty(callbackQuery.Data))
            {
                if (callbackQuery.Data.StartsWith('/'))
                    await Task.Run(() => Command.Parser.Parse(callbackQuery));
            }

            return;
        }
        
        private static async Task HandleUpdateAsync(Message message)
        {
            if (message.Date < DateTime.Now.AddHours(-pastHoursToSkip))
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Bot.Manager.BotClient_OnMessage",
                    Level = Models.SystemLog.Levels.Debug,
                    Message = $"Skipping update older than {pastHoursToSkip} hour(s)",
                    UserId = -1
                });
                return;
            }

            await Task.Run(CacheData.IncrementHandledMessages);
            
            if(CacheData.Groups.Keys.Contains(message.Chat.Id))
                if (CacheData.Groups[message.Chat.Id].State !=
                    Models.Group.TelegramGroup.Status.Active &&
                    message.Text != "/enable") return;

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban.Bot.Manager.BotClient_OnMessage",
                Level = Models.SystemLog.Levels.Debug,
                Message = "Message received",
                UserId = -1
            });

            await Task.Run(() => Functions.CacheUsername(message));

            if (message.MigrateToChatId is not null)
            {
                Functions.MigrateToChatId(message);
                message.Chat.Id = (long)message.MigrateToChatId;
            }

            bool isPrivateChat = message.Chat.Type == ChatType.Private ||
                                 message.Chat.Type == ChatType.Channel;
            if (isPrivateChat)
            {
                MessageQueueManager.AddChatIfNotPresent(message.Chat.Id);
            }

            bool justAdded = false;
            if (message.NewChatMembers != null)
            {
                justAdded = message.NewChatMembers.SingleOrDefault(x => x.Id == MyId) != null;
            }

            if (!justAdded && !isPrivateChat &&
                !CacheData.Groups.ContainsKey(message.Chat.Id))
            {
                var logMessage = string.Format(
                    "*[Alert]*\n" +
                    "Group *{0}* left due to missing group record in database.\n" +
                    "⚠ do not open links you don't know ⚠\n" +
                    "\nChat: `{1}`" +
                    "\n\n*hash_code:* #UB{2}-{3}",
                    message.Chat.Title,
                    message.Chat.Id,
                    message.Chat.Id.ToString().Replace("-", ""),
                    Guid.NewGuid());
                MessageQueueManager.EnqueueLog(new Models.ChatMessage()
                {
                    ParseMode = ParseMode.Markdown,
                    Text = logMessage
                });

                try
                {
                    await BotClient.SendTextMessageAsync(message.Chat.Id,
                        "We're sorry but an error has occurred while retrieving this chat on our database.\n" +
                        "Please add again the bot if you want to continue to use it.\n" +
                        "For any doubt reach us in our support group @unifiedban_group");
                }
                catch
                {
                    Data.Utils.Logging.AddLog(new Models.SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Unifiedban.Bot.Manager.BotClient_OnMessage",
                        Level = Models.SystemLog.Levels.Warn,
                        Message = "Can't send left notification due to missing permission.",
                        UserId = -1
                    });
                }

                await BotClient.LeaveChatAsync(message.Chat.Id);
                return;
            }

            if (!string.IsNullOrEmpty(message.Text) &&
                !Utils.UserTools.KickIfInBlacklist(message))
            {
                bool isCommand = false;
                if (message.Text.StartsWith('/'))
                {
                    isCommand = Command.Parser.Parse(message).Result;
                }

                if (message.ReplyToMessage != null && !isCommand && !isPrivateChat)
                {
                    if (message.ReplyToMessage.From.Id == MyId)
                    {
                        CommandQueueManager.ReplyMessage(message);
                        return;
                    }
                }

                if (!Utils.ChatTools.HandleSupportSessionMsg(message) && !isCommand &&
                    message.From.Id != 777000 && !isPrivateChat)  // Telegram's official updateServiceNotification
                {
                    Controls.Manager.DoCheck(message);
                }
            }
            if (message.NewChatMembers != null)
            {
                Functions.UserJoinedAction(message);
            }
            if (message.LeftChatMember != null)
            {
                Functions.UserLeftAction(message);
            }

            if (!string.IsNullOrEmpty(message.MediaGroupId) ||
                message.Photo != null ||
                message.Document != null)
            {
                Controls.Manager.DoMediaCheck(message);
            }

            if (!isPrivateChat && message.NewChatTitle != null)
            {
                CacheData.Groups[message.Chat.Id].Title = message.NewChatTitle;
            }
        }
    }
}
