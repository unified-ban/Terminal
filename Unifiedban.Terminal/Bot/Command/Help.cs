/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Bot.Command
{
    public class Help : ICommand
    {
        public void Execute(Message message)
        {
            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            string text = Utils.Parsers.VariablesParser(CacheData.SysConfigs
                .Single(x => x.SysConfigId == "HelpMenu")
                .Value);

            if (ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                text += Environment.NewLine;
                text += CacheData.SysConfigs
                    .Single(x => x.SysConfigId == "HelpMenuAdmin")
                    .Value;
            }
            
            if (BotTools.IsUserOperator(message.From.Id))
            {
                text += Environment.NewLine;
                text += CacheData.SysConfigs
                    .Single(x => x.SysConfigId == "HelpMenuOperatorBase")
                    .Value;
                
                if (BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Advanced))
                {
                    text += Environment.NewLine;
                    text += CacheData.SysConfigs
                        .Single(x => x.SysConfigId == "HelpMenuOperatorAdv")
                        .Value;
                }
            }
            
            text += Environment.NewLine;
            text += Environment.NewLine;
            text += "* usernames are saved in cache and never stored on database or file. The cache is cleared at every reboot or update.";
            
            MessageQueueManager.EnqueueMessage(
                new Models.ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Html,
                    Text = text
                });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
