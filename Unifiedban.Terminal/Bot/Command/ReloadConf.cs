using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class ReloadConf : ICommand
    {
        public void Execute(Message message)
        {
            {

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
    }
}
