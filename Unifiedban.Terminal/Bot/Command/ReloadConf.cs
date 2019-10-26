using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Unifiedban.Terminal.Bot.Command
{
    public class ReloadConf : ICommand
    {
        public void Execute(Message message)
        {
            if(CacheData.Operators
                .SingleOrDefault(x => x.TelegramUserId == message.From.Id 
                && x.Level == Models.Operator.Levels.Super) == null)
            {
                MessageQueueManager.EnqueueMessage(
                   new ChatMessage()
                   {
                       Timestamp = DateTime.UtcNow,
                       Chat = message.Chat,
                       ReplyToMessageId = message.MessageId,
                       Text = CacheData.GetTranslation("en", "error_not_auth_command")
                   });
                Manager.BotClient.SendTextMessageAsync(
                    chatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "User *{0}:{1}* tried to use command ReloadConf.",
                        message.From.Id,
                        message.From.Username)
                );
                return;
            }

            try
            {
                BusinessLogic.SysConfigLogic sysConfigLogic = new BusinessLogic.SysConfigLogic();
                CacheData.SysConfigs = new List<Models.SysConfig>(sysConfigLogic.Get());

                Manager.BotClient.SendTextMessageAsync(
                    chatId: Convert.ToInt64(CacheData.SysConfigs
                                .Single(x => x.SysConfigId == "ControlChatId")
                                .Value),
                    text: "Conf reloaded successfully."
                );
            }
            catch (Exception ex)
            {
                Data.Utils.Logging.AddLog(new Models.SystemLog()
                {
                    LoggerName = CacheData.LoggerName,
                    Date = DateTime.Now,
                    Function = "Unifiedban.Terminal.Command.ReloadConf",
                    Level = Models.SystemLog.Levels.Error,
                    Message = ex.Message,
                    UserId = -1
                });

                Manager.BotClient.SendTextMessageAsync(
                    chatId: Convert.ToInt64(CacheData.SysConfigs
                            .Single(x => x.SysConfigId == "ControlChatId")
                            .Value),
                    text: "Error reloading conf. Check logs."
                );
            }
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            return;
        }
    }
}
