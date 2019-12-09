using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Unifiedban.Terminal.Bot.Command
{
    public class TestCommand : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            if (!Utils.BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Super))
            {
                return;
            }

            List<InlineKeyboardButton> confirmationButton = new List<InlineKeyboardButton>();
            confirmationButton.Add(InlineKeyboardButton.WithUrl("Start with command", "http://t.me/LinuxPixelHubBot?start=motd"));
            confirmationButton.Add(InlineKeyboardButton.WithCallbackData(
                                        CacheData.GetTranslation("en", "captcha_iamhuman", true),
                                        $"/test " + message.From.Id
                                        ));

            MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ParseMode = ParseMode.Markdown,
                        Text = "*[ADMIN]*\nSelect a test",
                        ReplyMarkup = new InlineKeyboardMarkup(
                            confirmationButton
                        )
                    });

            MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = Newtonsoft.Json.JsonConvert.SerializeObject(confirmationButton)
                    });
        }

        public void Execute(CallbackQuery callbackQuery)
        {
            if (!Utils.BotTools.IsUserOperator(callbackQuery.From.Id, Models.Operator.Levels.Super))
            {
                return;
            }
        }
    }
}
