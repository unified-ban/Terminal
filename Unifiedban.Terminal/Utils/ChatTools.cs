
/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Quartz;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Unifiedban.Data.Utils;
using Unifiedban.Models;
using Unifiedban.Models.Group;
using Unifiedban.Terminal.Bot;
using Unifiedban.Terminal.Jobs;

namespace Unifiedban.Terminal.Utils
{
    public class ChatTools
    {
        static Dictionary<long, DateTime> lastOperatorSupportMsg = new Dictionary<long, DateTime>();
        static Dictionary<long, ChatPermissions> chatPermissionses = new Dictionary<long, ChatPermissions>();
        private static bool _firstCycle = true;
        public static void Initialize()
        {
            // RecurringJob.AddOrUpdate("ChatTools_CheckNightSchedule", () => CheckNightSchedule(), "0 * * ? * *");
            // RecurringJob.Trigger("ChatTools_CheckNightSchedule");
            
            // RecurringJob.AddOrUpdate("ChatTools_RenewInviteLinks", () => RenewInviteLinks(), "0 0/5 * ? * *");
            //RecurringJob.Trigger("ChatTools_RenewInviteLinks");

            var chatToolsJob = JobBuilder.Create<ChatToolsJob>()
                .WithIdentity("chatToolsJob", "chatTools")
                .Build();
            var chatToolsJobTrigger = TriggerBuilder.Create()
                .WithIdentity("chatToolsJobTrigger", "chatTools")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(30)
                    .RepeatForever())
                .Build();
            Program.Scheduler?.ScheduleJob(chatToolsJob, chatToolsJobTrigger).Wait();
            
            Logging.AddLog(new SystemLog
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = SystemLog.Levels.Info,
                Message = "Chat Tools initialized",
                UserId = -2
            });
        }

        public static bool IsUserAdmin(long chatId, long userId)
        {
            try
            {
                var administrators = Manager.BotClient.GetChatAdministratorsAsync(chatId).Result;
                foreach (ChatMember member in administrators)
                {
                    if (member.User.Id == userId)
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public static List<long> GetChatAdminIds(long chatId)
        {
            var admins = new List<long>();
            var administrators = Manager.BotClient.GetChatAdministratorsAsync(chatId).Result;
            for (var index = 0; index < administrators.Length; index++)
            {
                ChatMember member = administrators[index];
                admins.Add(member.User.Id);
            }

            return admins;
        }
        
        public static bool HandleSupportSessionMsg(Message message)
        {
            if (!CacheData.ActiveSupport
                .Contains(message.Chat.Id))
                return false;

            if (!lastOperatorSupportMsg.ContainsKey(message.Chat.Id))
            {
                lastOperatorSupportMsg[message.Chat.Id] = DateTime.UtcNow;
            }

            bool isFromOperator = false;
            if (BotTools.IsUserOperator(message.From.Id))
            {
                isFromOperator = true;
                Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                ChatMessage newMsg = new ChatMessage
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Html,
                    Text = message.Text +
                        "\n\nMessage from operator: <b>" + message.From.Username + "</b>"
                };
                if (message.ReplyToMessage != null)
                    newMsg.ReplyToMessageId = message.ReplyToMessage.MessageId;
                MessageQueueManager.EnqueueMessage(newMsg);
                lastOperatorSupportMsg[message.Chat.Id] = DateTime.UtcNow;
            }

            var timeDifference = DateTime.UtcNow - lastOperatorSupportMsg[message.Chat.Id];
            if (timeDifference.Minutes >= 3 && timeDifference.Minutes < 5)
            {
                ChatMessage newMsg = new ChatMessage
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = "*[Alert]*" +
                           "\n\nSupport session is going to be automatically closed in 2 minutes " +
                           "due to operator's inactivity"
                };
                MessageQueueManager.EnqueueMessage(newMsg);
            }
            if (timeDifference.Minutes >= 5)
            {
                ChatMessage newMsg = new ChatMessage
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    ParseMode = ParseMode.Markdown,
                    Text = "*[Alert]*" +
                           "\n\nSupport session is closed due to operator's inactivity"
                };
                MessageQueueManager.EnqueueMessage(newMsg);
                CacheData.ActiveSupport.Remove(message.Chat.Id);
                CacheData.CurrentChatAdmins.Remove(message.Chat.Id);
                
                MessageQueueManager.EnqueueLog(new ChatMessage
                {
                    ParseMode = ParseMode.Markdown,
                    Text = String.Format(
                        "*[Log]*" +
                        "\n\nSupport session ended due to operator's inactivity" +
                        "\nChatId: `{0}`" +
                        "\nChat: `{1}`" +
                        "\n\n*hash_code:* #UB{2}-{3}",
                        message.Chat.Id,
                        message.Chat.Title,
                        message.Chat.Id.ToString().Replace("-", ""),
                        Guid.NewGuid())
                });
            }

            Task.Run(() => RecordSupportSessionMessage(message));

            return isFromOperator;
        }

        private static void RecordSupportSessionMessage(Message message)
        {
            SupportSessionLog.SenderType senderType = SupportSessionLog.SenderType.User;
            if (BotTools.IsUserOperator(message.From.Id))
                senderType = SupportSessionLog.SenderType.Operator;
            else if (CacheData.CurrentChatAdmins[message.Chat.Id]
                    .Contains(message.From.Id))
                senderType = SupportSessionLog.SenderType.Admin;

            LogTools.AddSupportSessionLog(new SupportSessionLog
            {
                GroupId = CacheData.Groups[message.Chat.Id].GroupId,
                SenderId = message.From.Id,
                Text = message.Text,
                Timestamp = DateTime.UtcNow,
                Type = senderType
            });
        }

        internal static void CheckNightSchedule()
        {
            List<NightSchedule> activeSchedules =
                CacheData.NightSchedules.Values
                    .Where(x => x.State != NightSchedule.Status.Deactivated)
                    .ToList();
            CloseGroups(activeSchedules);
            OpenGroups(activeSchedules);
            if(_firstCycle) _firstCycle = false;
        }

        private static void CloseGroups(List<NightSchedule> nightSchedules)
        {
            foreach(NightSchedule nightSchedule in nightSchedules)
            {
                var chatId = CacheData.Groups.Values
                    .Single(x => x.GroupId == nightSchedule.GroupId).TelegramChatId;
                
                if(CacheData.Groups[chatId].State != TelegramGroup.Status.Active)
                    continue;
                if (CacheData.NightSchedules[nightSchedule.GroupId].State != NightSchedule.Status.Programmed)
                    continue;

                if (_firstCycle)
                {
                    TimeSpan diffStartDate = DateTime.UtcNow - nightSchedule.UtcStartDate.Value;
                    if (diffStartDate.Days > 0)
                    {
                        CacheData.NightSchedules[nightSchedule.GroupId].UtcStartDate =
                            CacheData.NightSchedules[nightSchedule.GroupId].UtcStartDate.Value
                                .AddDays(diffStartDate.Days);
                        continue;
                    }
                }

                if(nightSchedule.UtcStartDate.Value <= DateTime.UtcNow)
                {
                    try
                    {
                        CacheData.NightSchedules[nightSchedule.GroupId].State = NightSchedule.Status.Active;
                        chatPermissionses[chatId] = Manager.BotClient.GetChatAsync(chatId).Result.Permissions;

                        Manager.BotClient.SetChatPermissionsAsync(chatId,
                            new ChatPermissions
                            {
                                CanSendMessages = false,
                                CanAddWebPagePreviews = false,
                                CanChangeInfo = false,
                                CanInviteUsers = false,
                                CanPinMessages = false,
                                CanSendMediaMessages = false,
                                CanSendOtherMessages = false,
                                CanSendPolls = false
                            });

                        MessageQueueManager.EnqueueMessage(
                            new ChatMessage
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = new Chat {Id = chatId, Type = ChatType.Supergroup},
                                Text = "The group is now closed as per Night Schedule settings."
                            });
                        CacheData.NightSchedules[nightSchedule.GroupId].UtcStartDate =
                            CacheData.NightSchedules[nightSchedule.GroupId].UtcStartDate.Value.AddDays(1);
                    }
                    catch (Exception ex)
                    {
                        Logging.AddLog(new SystemLog
                        {
                            LoggerName = CacheData.LoggerName,
                            Date = DateTime.Now,
                            Function = "Utils.ChatTools.CloseGroups",
                            Level = SystemLog.Levels.Error,
                            Message = $"Exception on ChatId: {chatId}",
                            UserId = -2
                        });
                        Logging.AddLog(new SystemLog
                        {
                            LoggerName = CacheData.LoggerName,
                            Date = DateTime.Now,
                            Function = "Utils.ChatTools.CloseGroups",
                            Level = SystemLog.Levels.Error,
                            Message = $"{ex.Message}\n{ex.InnerException?.Message}",
                            UserId = -2
                        });
                        
                        if (ex.Message.Contains("chat not found") ||
                            ex.Message.Contains("chat was deleted") ||
                            ex.Message.Contains("bot was kicked"))
                        {
                            var log = new SystemLog()
                            {
                                LoggerName = CacheData.LoggerName,
                                Date = DateTime.Now,
                                Function = "Utils.ChatTools.CloseGroups",
                                Level = SystemLog.Levels.Error,
                                Message = $"Disabling GroupId: {nightSchedule.GroupId} - {chatId}",
                                UserId = -1
                            };
                            Logging.AddLog(log);
                            // LogTools.AddSystemLog(log);
                            LogTools.AddActionLog(new ActionLog
                            {
                                ActionTypeId = "autoDisable",
                                GroupId = nightSchedule.GroupId,
                                Parameters = ex.Message,
                                UtcDate = DateTime.UtcNow
                            });
                    
                            CacheData.Groups[chatId].State = TelegramGroup.Status.Inactive;
                        }
                    }
                }
            }
        }

        private static void OpenGroups(List<NightSchedule> nightSchedules)
        {
            foreach (NightSchedule nightSchedule in nightSchedules)
            {
                var chatId = CacheData.Groups.Values
                    .Single(x => x.GroupId == nightSchedule.GroupId).TelegramChatId;
                
                if(CacheData.Groups[chatId].State != TelegramGroup.Status.Active)
                    continue;
                if (CacheData.NightSchedules[nightSchedule.GroupId].State != NightSchedule.Status.Active)
                    continue;

                if (_firstCycle)
                {
                    TimeSpan diffEndDays = DateTime.UtcNow - nightSchedule.UtcEndDate.Value;
                    if (diffEndDays.Days > 0)
                    {
                        CacheData.NightSchedules[nightSchedule.GroupId].UtcEndDate =
                            CacheData.NightSchedules[nightSchedule.GroupId].UtcEndDate.Value
                                .AddDays(diffEndDays.Days);

                        if (CacheData.NightSchedules[nightSchedule.GroupId].UtcEndDate.Value < DateTime.UtcNow.Date)
                        {
                            CacheData.NightSchedules[nightSchedule.GroupId].UtcEndDate =
                            CacheData.NightSchedules[nightSchedule.GroupId].UtcEndDate.Value
                                .AddDays(1);
                        }
                        continue;
                    }
                }

                if (nightSchedule.UtcEndDate.Value <= DateTime.UtcNow)
                {
                    try
                    {
                        CacheData.NightSchedules[nightSchedule.GroupId].State = NightSchedule.Status.Programmed;

                        if (chatPermissionses.ContainsKey(chatId))
                        {
                            Manager.BotClient.SetChatPermissionsAsync(chatId,
                                chatPermissionses[chatId]);
                        }
                        else
                        {
                            MessageQueueManager.EnqueueMessage(
                                new ChatMessage
                                {
                                    Timestamp = DateTime.UtcNow,
                                    Chat = new Chat {Id = chatId, Type = ChatType.Supergroup},
                                    Text = "*[Report]*\nImpossible to find previous permissions set.\n" +
                                           "Using default value (all true except CanChangeInfo)."
                                });
                            Manager.BotClient.SetChatPermissionsAsync(chatId,
                                new ChatPermissions
                                {
                                    CanSendMessages = true,
                                    CanAddWebPagePreviews = true,
                                    CanChangeInfo = false,
                                    CanInviteUsers = true,
                                    CanPinMessages = true,
                                    CanSendMediaMessages = true,
                                    CanSendOtherMessages = true,
                                    CanSendPolls = true
                                });
                        }

                        MessageQueueManager.EnqueueMessage(
                            new ChatMessage
                            {
                                Timestamp = DateTime.UtcNow,
                                Chat = new Chat {Id = chatId, Type = ChatType.Supergroup},
                                Text = "The group is now open as per Night Schedule settings."
                            });
                        CacheData.NightSchedules[nightSchedule.GroupId].UtcEndDate =
                            CacheData.NightSchedules[nightSchedule.GroupId].UtcEndDate.Value.AddDays(1);
                    }
                    catch (Exception ex)
                    {
                        Logging.AddLog(new SystemLog
                        {
                            LoggerName = CacheData.LoggerName,
                            Date = DateTime.Now,
                            Function = "Utils.ChatTools.OpenGroups",
                            Level = SystemLog.Levels.Error,
                            Message = $"Exception on ChatId: {chatId}",
                            UserId = -2
                        });
                        Logging.AddLog(new SystemLog
                        {
                            LoggerName = CacheData.LoggerName,
                            Date = DateTime.Now,
                            Function = "Utils.ChatTools.OpenGroups",
                            Level = SystemLog.Levels.Error,
                            Message = $"{ex.Message}\n{ex.InnerException?.Message}",
                            UserId = -2
                        });
                        
                        if (ex.Message.Contains("chat not found") ||
                            ex.Message.Contains("chat was deleted") ||
                            ex.Message.Contains("bot was kicked"))
                        {
                            var log = new SystemLog()
                            {
                                LoggerName = CacheData.LoggerName,
                                Date = DateTime.Now,
                                Function = "Utils.ChatTools.OpenGroups",
                                Level = SystemLog.Levels.Error,
                                Message = $"Disabling GroupId: {nightSchedule.GroupId} - {chatId}",
                                UserId = -1
                            };
                            Logging.AddLog(log);
                            // LogTools.AddSystemLog(log);
                            LogTools.AddActionLog(new ActionLog
                            {
                                ActionTypeId = "autoDisable",
                                GroupId = nightSchedule.GroupId,
                                Parameters = ex.Message,
                                UtcDate = DateTime.UtcNow
                            });
                    
                            CacheData.Groups[chatId].State = TelegramGroup.Status.Inactive;
                        }
                    }
                }
            }
        }

        public static int SendCaptchaImage(ChatId chatId, string name, long memberId, string timerKey)
        {
            Random rnd = new Random();

            var num1 = rnd.Next(0, 11);
            var num2 = rnd.Next(0, 11);
            var operation = rnd.Next(1, 4);
            var symbol = "+";
            switch (operation)
            {
                case 1:
                    symbol = "+";
                    break;
                case 2:
                    symbol = "-";
                    break;
                case 3:
                    symbol = "x";
                    break;
            }

            var text = $"{num1} {symbol} {num2}";

            var correctPosition = rnd.Next(1, 4);
            var result = GetResult(num1, num2, symbol);
            var img = ConvertTextToImage(text, "Indie Flower", 60, Color.AntiqueWhite, Color.Black, 512, 256);
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                try
                {
                    return Manager.BotClient.SendPhotoAsync(chatId, new InputOnlineFile(ms),
                        caption: CacheData.GetTranslation(CacheData.Groups[chatId.Identifier ?? 0].SettingsLanguage,
                            "captcha_iamhuman_img", true).Replace("{{name}}", name),
                        parseMode: ParseMode.Markdown,
                        disableNotification: true,
                        replyMarkup: BuildCaptchaButtons(result, memberId, timerKey)).Result.MessageId;
                }
                catch (Exception ex)
                {
                    Logging.AddLog(new SystemLog
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "ChatTools.SendCaptchaImage",
                        Level = SystemLog.Levels.Error,
                        Message = ex.Message,
                        UserId = -1
                    });
                    if(ex.InnerException != null)
                        Logging.AddLog(new SystemLog
                        {
                            LoggerName = CacheData.LoggerName,
                            Date = DateTime.Now,
                            Function = "ChatTools.SendCaptchaImage",
                            Level = SystemLog.Levels.Error,
                            Message = ex.InnerException.Message,
                            UserId = -1
                        });
                    return -1;
                }
            }
        }
        
        private static Bitmap ConvertTextToImage(string txt, string fontname, int fontsize, Color bgcolor, Color fcolor, int width, int Height)
        {
            Bitmap bmp = new Bitmap(width, Height);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {

                Font font = new Font(fontname, fontsize);
                graphics.FillRectangle(new SolidBrush(bgcolor), 0, 0, bmp.Width, bmp.Height);
                graphics.DrawString(txt, font, new SolidBrush(fcolor), 150, (256/2-60));
                graphics.Flush();
                font.Dispose();
                graphics.Dispose();
            }
            return bmp;
        }
        
        private static int GetResult(int num1, int num2, string symbol)
        {
            switch (symbol)
            {
                case "+":
                    return num1 + num2;
                case "-":
                    return num1 - num2;
                case "x":
                    return num1 * num2;
            }

            return default;
        }

        private static string GetRandomEmoji()
        {
            string[] emojis = {
	            "😄","😃","😀","😊","☺","😉","😍","😘","😚","😗","😙","😜","😝","😛","😳","😁","😔","😌","😒","😞","😣","😢","😂","😭","😪","😥","😰","😅","😓","😩","😫","😨","😱","😠","😡","😤","😖","😆","😋","😷","😎","😴","😵","😲","😟","😦","😧","😈","👿","😮","😬","😐","😕","😯","😶","😇","😏","😑","👲","👳","👮","👷","💂","👶","👦","👧","👨","👩","👴","👵","👱","👼","👸","😺","😸","😻","😽","😼","🙀","😿","😹","😾","👹","👺","🙈","🙉","🙊","💀","👽","💩","🔥","✨","🌟","💫","💥","💢","💦","💧","💤","💨","👂","👀","👃","👅","👄","👍","👎","👌","👊","✊","✌","👋","✋","👐","👆","👇","👉","👈","🙌","🙏","☝","👏","💪","🚶","🏃","💃","👫","👪","👬","👭","💏","💑","👯","🙆","🙅","💁","🙋","💆","💇","💅","👰","🙎","🙍","🙇","🎩","👑","👒","👟","👞","👡","👠","👢","👕","👔","👚","👗","🎽","👖","👘","👙","💼","👜","👝","👛","👓","🎀","🌂","💄","💛","💙","💜","💚","❤","💔","💗","💓","💕","💖","💞","💘","💌","💋","💍","💎","👤","👥","💬","👣","💭","🐶","🐺","🐱","🐭","🐹","🐰","🐸","🐯","🐨","🐻","🐷","🐽","🐮","🐗","🐵","🐒","🐴","🐑","🐘","🐼","🐧","🐦","🐤","🐥","🐣","🐔","🐍","🐢","🐛","🐝","🐜","🐞","🐌","🐙","🐚","🐠","🐟","🐬","🐳","🐋","🐄","🐏","🐀","🐃","🐅","🐇","🐉","🐎","🐐","🐓","🐕","🐖","🐁","🐂","🐲","🐡","🐊","🐫","🐪","🐆","🐈","🐩","🐾","💐","🌸","🌷","🍀","🌹","🌻","🌺","🍁","🍃","🍂","🌿","🌾","🍄","🌵","🌴","🌲","🌳","🌰","🌱","🌼","🌐","🌞","🌝","🌚","🌑","🌒","🌓","🌔","🌕","🌖","🌗","🌘","🌜","🌛","🌙","🌍","🌎","🌏","🌋","🌌","🌠","⭐","☀","⛅","☁","⚡","☔","❄","⛄","🌀","🌁","🌈","🌊","🎍","💝","🎎","🎒","🎓","🎏","🎆","🎇","🎐","🎑","🎃","👻","🎅","🎄","🎁","🎋","🎉","🎊","🎈","🎌","🔮","🎥","📷","📹","📼","💿","📀","💽","💾","💻","📱","☎","📞","📟","📠","📡","📺","📻","🔊","🔉","🔈","🔇","🔔","🔕","📢","📣","⏳","⌛","⏰","⌚","🔓","🔒","🔏","🔐","🔑","🔎","💡","🔦","🔆","🔅","🔌","🔋","🔍","🛁","🛀","🚿","🚽","🔧","🔩","🔨","🚪","🚬","💣","🔫","🔪","💊","💉","💰","💴","💵","💷","💶","💳","💸","📲","📧","📥","📤","✉","📩","📨","📯","📫","📪","📬","📭","📮","📦","📝","📄","📃","📑","📊","📈","📉","📜","📋","📅","📆","📇","📁","📂","✂","📌","📎","✒","✏","📏","📐","📕","📗","📘","📙","📓","📔","📒","📚","📖","🔖","📛","🔬","🔭","📰","🎨","🎬","🎤","🎧","🎼","🎵","🎶","🎹","🎻","🎺","🎷","🎸","👾","🎮","🃏","🎴","🀄","🎲","🎯","🏈","🏀","⚽","⚾","🎾","🎱","🏉","🎳","⛳","🚵","🚴","🏁","🏇","🏆","🎿","🏂","🏊","🏄","🎣","☕","🍵","🍶","🍼","🍺","🍻","🍸","🍹","🍷","🍴","🍕","🍔","🍟","🍗","🍖","🍝","🍛","🍤","🍱","🍣","🍥","🍙","🍘","🍚","🍜","🍲","🍢","🍡","🍳","🍞","🍩","🍮","🍦","🍨","🍧","🎂","🍰","🍪","🍫","🍬","🍭","🍯","🍎","🍏","🍊","🍋","🍒","🍇","🍉","🍓","🍑","🍈","🍌","🍐","🍍","🍠","🍆","🍅","🌽","🏠","🏡","🏫","🏢","🏣","🏥","🏦","🏪","🏩","🏨","💒","⛪","🏬","🏤","🌇","🌆","🏯","🏰","⛺","🏭","🗼","🗾","🗻","🌄","🌅","🌃","🗽","🌉","🎠","🎡","⛲","🎢","🚢","⛵","🚤","🚣","⚓","🚀","✈","💺","🚁","🚂","🚊","🚉","🚞","🚆","🚄","🚅","🚈","🚇","🚝","🚋","🚃","🚎","🚌","🚍","🚙","🚘","🚗","🚕","🚖","🚛","🚚","🚨","🚓","🚔","🚒","🚑","🚐","🚲","🚡","🚟","🚠","🚜","💈","🚏","🎫","🚦","🚥","⚠","🚧","🔰","⛽","🏮","🎰","♨","🗿","🎪","🎭","📍","🚩","⬆","⬇","⬅","➡","🔠","🔡","🔤","↗","↖","↘","↙","↔","↕","🔄","◀","▶","🔼","🔽","↩","↪","ℹ","⏪","⏩","⏫","⏬","⤵","⤴","🆗","🔀","🔁","🔂","🆕","🆙","🆒","🆓","🆖","📶","🎦","🈁","🈯","🈳","🈵","🈴","🈲","🉐","🈹","🈺","🈶","🈚","🚻","🚹","🚺","🚼","🚾","🚰","🚮","🅿","♿","🚭","🈷","🈸","🈂","Ⓜ","🛂","🛄","🛅","🛃","🉑","㊙","㊗","🆑","🆘","🆔","🚫","🔞","📵","🚯","🚱","🚳","🚷","🚸","⛔","✳","❇","❎","✅","✴","💟","🆚","📳","📴","🅰","🅱","🆎","🅾","💠","➿","♻","♈","♉","♊","♋","♌","♍","♎","♏","♐","♑","♒","♓","⛎","🔯","🏧","💹","💲","💱","©","®","™","〽","〰","🔝","🔚","🔙","🔛","🔜","❌","⭕","❗","❓","❕","❔","🔃","🕛","🕧","🕐","🕜","🕑","🕝","🕒","🕞","🕓","🕟","🕔","🕠","🕕","🕖","🕗","🕘","🕙","🕚","🕡","🕢","🕣","🕤","🕥","🕦","✖","➕","➖","➗","♠","♥","♣","♦","💮","💯","✔","☑","🔘","🔗","➰","🔱","🔲","🔳","◼","◻","◾","◽","▪","▫","🔺","⬜","⬛","⚫","⚪","🔴","🔵","🔻","🔶","🔷","🔸","🔹"
            };

            Random rnd = new Random();
            var index = rnd.Next(0, emojis.Length);
            return emojis[index];
        }

        private static InlineKeyboardMarkup BuildCaptchaButtons(int result, long memberId, string timerKey)
        {
            Random rnd = new Random();
            var correctPosition = rnd.Next(1, 4);

            List<InlineKeyboardButton> buttons;
            switch (correctPosition)
            {
                default:
                    buttons = new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData(
                            result.ToString(),
                            $"/Captcha {memberId} {timerKey}"
                        ),
                        InlineKeyboardButton.WithCallbackData(GetRandomEmoji()),
                        InlineKeyboardButton.WithCallbackData(GetRandomEmoji())
                    };
                    break;
                case 2:
                    buttons = new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData(GetRandomEmoji()),
                        InlineKeyboardButton.WithCallbackData(
                            result.ToString(),
                            $"/Captcha {memberId} {timerKey}"
                        ),
                        InlineKeyboardButton.WithCallbackData(GetRandomEmoji())
                    };
                    break;
                case 3:
                    buttons = new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData(GetRandomEmoji(), $"/CaptchaError {memberId} {timerKey}"),
                        InlineKeyboardButton.WithCallbackData(GetRandomEmoji(), $"/CaptchaError {memberId} {timerKey}"),
                        InlineKeyboardButton.WithCallbackData(
                            result.ToString(),
                            $"/Captcha {memberId} {timerKey}"
                        )
                    };
                    break;
            }

            return new InlineKeyboardMarkup(buttons);
        }

        internal static async Task RenewInviteLinks()
        {
            var groups = new TelegramGroup[CacheData.Groups.Count];
            lock (CacheData.GroupsLockObj)
            {
                CacheData.Groups.Values.CopyTo(groups, 0);
            }

            foreach (var telegramGroup in groups
                         .Where(x => x.State == TelegramGroup.Status.Active && x.InviteAlias != null))
            {
                try
                {
                    if (string.IsNullOrEmpty(telegramGroup.InviteAlias)) continue;
                    var meInChat = await Manager.BotClient.GetChatMemberAsync(telegramGroup.TelegramChatId, Manager.MyId);
                    if (meInChat is ChatMemberAdministrator { CanInviteUsers: true })
                    {
                        CacheData.Groups[telegramGroup.TelegramChatId].InviteLink =
                            await Manager.BotClient.ExportChatInviteLinkAsync(telegramGroup.TelegramChatId);
                    }
                }
                catch (Exception ex)
                {
                    Logging.AddLog(new SystemLog
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "ChatTools.RenewInviteLinks",
                        Level = SystemLog.Levels.Warn,
                        Message = $"Error exporting new chat (id {telegramGroup.TelegramChatId}) invite link due to exception: {ex.Message}",
                        UserId = -1
                    });

                    if (ex.Message.Contains("chat not found"))
                    {
                        CacheData.Groups[telegramGroup.TelegramChatId].State = TelegramGroup.Status.Inactive;
                        Logging.AddLog(new SystemLog
                        {
                            LoggerName = CacheData.LoggerName,
                            Date = DateTime.Now,
                            Function = "ChatTools.RenewInviteLinks",
                            Level = SystemLog.Levels.Info,
                            Message = $"Chat id {telegramGroup.TelegramChatId} disabled. Looks like we're not there anymore.",
                            UserId = -1
                        });
                    }
                }
                Thread.Sleep(100);
            }
        }
    }
}
