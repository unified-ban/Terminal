/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Unifiedban.Terminal.Utils;


namespace Unifiedban.Terminal.Bot.Command
{
    public class CaptchaError : ICommand
    {
        public void Execute(Message message) { }

        public void Execute(CallbackQuery callbackQuery)
        {
            string[] args = callbackQuery.Data.Split(" ");
            long captchaId = Convert.ToInt64(args[1]);

            if (captchaId != callbackQuery.From.Id)
                return;

            if (CacheData.CaptchaStrikes.ContainsKey(callbackQuery.From.Id))
            {
                CacheData.CaptchaStrikes[callbackQuery.From.Id] += 1;
            }
            else
            {
                CacheData.CaptchaStrikes[callbackQuery.From.Id] = 1;
            }

            if (CacheData.CaptchaStrikes[callbackQuery.From.Id] >= 2)
            {
                Manager.BotClient.DeleteMessageAsync(
                    callbackQuery.Message.Chat.Id,
                    callbackQuery.Message.MessageId);
                
                try
                {
                    Manager.BotClient.KickChatMemberAsync(callbackQuery.Message.Chat.Id, callbackQuery.From.Id);
                    if (callbackQuery.Message.Chat.Type == ChatType.Supergroup)
                        Manager.BotClient.UnbanChatMemberAsync(callbackQuery.Message.Chat.Id, callbackQuery.From.Id);
                }
                catch
                {
                    MessageQueueManager.EnqueueMessage(
                        new Models.ChatMessage()
                        {
                            Timestamp = DateTime.UtcNow,
                            Chat = callbackQuery.Message.Chat,
                            ParseMode = ParseMode.Markdown,
                            Text = CacheData.GetTranslation(CacheData.Groups[callbackQuery.Message.Chat.Id].SettingsLanguage, "command_kick_error")
                        });
                    return;
                }
                
                UserTools.AddPenalty(callbackQuery.Message.Chat.Id, callbackQuery.From.Id,
                    Models.TrustFactorLog.TrustFactorAction.kick, Manager.MyId);
                
                if (args.Length > 2)
                {
                    if (CacheData.CaptchaAutoKickTimers.ContainsKey(args[2]))
                    {
                        CacheData.CaptchaAutoKickTimers[args[2]].Stop();
                        CacheData.CaptchaAutoKickTimers.TryRemove(args[2], out var timer);
                    }
                }
            }
        }
    }
}
