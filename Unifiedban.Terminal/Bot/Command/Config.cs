using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Config : ICommand
    {
        public void Execute(Message message)
        {
            Execute(message, false);
        }

        public void Execute(Message message, bool isUpdate = false)
        {
            if (!Utils.BotTools.IsUserOperator(message.From.Id) &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return;
            }

            List<List<InlineKeyboardButton>> configMenu = new List<List<InlineKeyboardButton>>();
            int btnCount = 0;
            int depthLevel = 0;

            configMenu.Add(new List<InlineKeyboardButton>());
            configMenu[0].Add(InlineKeyboardButton.WithUrl("Instructions", "https://unifiedban.solutions/?p=faq#FAQ_configure_instructions"));
            configMenu[0].Add(InlineKeyboardButton.WithUrl("Dashboard", "https://unifiedban.solutions/"));
            btnCount = 2;

            try
            {
                foreach (Models.Group.ConfigurationParameter conf in CacheData.GroupConfigs[message.Chat.Id])
                {
                    if (btnCount == 2)
                    {
                        btnCount = 0;
                        configMenu.Add(new List<InlineKeyboardButton>());
                        depthLevel++;
                    }

                    switch (conf.Type)
                    {
                        case "bool":
                            string newSet = conf.Value == "true" ? "false" : "true";
                            string icon = conf.Value == "true" ? " ✅" : " ❌";
                            configMenu[depthLevel].Add(InlineKeyboardButton.WithCallbackData(
                                CacheData.GetTranslation("en", conf.ConfigurationParameterId, true) + icon,
                                $"/config { conf.ConfigurationParameterId }|{ newSet }"
                                ));
                            break;
                    }

                    btnCount++;
                }
            }
            catch
            {

            }

            configMenu.Add(new List<InlineKeyboardButton>());
            configMenu[depthLevel + 1].Add(InlineKeyboardButton.WithUrl("Ask for support", "https://t.me/unifiedban_group"));
            configMenu[depthLevel + 1].Add(InlineKeyboardButton.WithUrl("FAQ", "https://unifiedban.solutions/?p=faq"));
            configMenu.Add(new List<InlineKeyboardButton>());
            configMenu[depthLevel + 2].Add(InlineKeyboardButton.WithCallbackData("Close menu", $"/config close"));

            if (isUpdate)
            {
                Manager.BotClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    text: "*[Config:]*",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(
                            configMenu
                        )
                    );
                return;
            }
            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    Text = "*Settings*\nBot configuration for this group.",
                    ReplyMarkup = new InlineKeyboardMarkup(
                            configMenu
                        )
                });
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            if (!Utils.BotTools.IsUserOperator(callbackQuery.Message.From.Id) &&
                !Utils.ChatTools.IsUserAdmin(callbackQuery.Message.Chat.Id, callbackQuery.Message.From.Id))
            {
                return;
            }

            string[] data = callbackQuery.Data.Split(" ");
            if (data.Length < 2)
                return;

            string[] parameters = data[1].Split("|");
            if (parameters.Length < 1)
                return;
            switch (parameters[0])
            {
                case "close":
                    Manager.BotClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
                    break;
                default:
                    if (parameters.Length < 2)
                        return;
                    updateSetting(callbackQuery.Message, parameters[0], parameters[1]);
                    break;
            }
        }

        private void updateSetting(
            Message message, string configurationParameterId, string newValue)
        {
            Models.Group.ConfigurationParameter config = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == configurationParameterId)
                .SingleOrDefault();
            if (config == null)
                return;

            // TODO - check if value is in allowed range
            CacheData.GroupConfigs[message.Chat.Id]
                [CacheData.GroupConfigs[message.Chat.Id]
                .IndexOf(config)]
                .Value = newValue;

            Execute(message, true);
        }
    }
}
