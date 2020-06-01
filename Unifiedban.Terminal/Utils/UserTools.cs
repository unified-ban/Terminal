using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Unifiedban.Models;

namespace Unifiedban.Terminal.Utils
{
    public class UserTools
    {
        private static BusinessLogic.User.TrustFactorLogic tfl =
            new BusinessLogic.User.TrustFactorLogic();

        public static bool NameIsRTL(string fullName)
        {
            string regex = @"[\u0591-\u07FF]+";

            Regex reg = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            MatchCollection matchedWords = reg.Matches(fullName);
            if (matchedWords.Count > 0)
                return true;

            return false;
        }

        public static void AddPenality(int telegramUserId,
            Models.TrustFactorLog.TrustFactorAction action,
            int actionTakenBy)
        {
            int penality = 0;
            switch (action)
            {
                default:
                case Models.TrustFactorLog.TrustFactorAction.limit:
                    penality = int.Parse(CacheData.Configuration["TFLimitPenality"]);
                    break;
                case Models.TrustFactorLog.TrustFactorAction.kick:
                    penality = int.Parse(CacheData.Configuration["TFKickPenality"]);
                    break;
                case Models.TrustFactorLog.TrustFactorAction.ban:
                    penality = int.Parse(CacheData.Configuration["TFBanPenality"]);
                    break;
                case Models.TrustFactorLog.TrustFactorAction.blacklist:
                    penality = CacheData.TrustFactors[telegramUserId].Points;
                    break;
            }

            if (!CacheData.TrustFactors.ContainsKey(telegramUserId))
            {
                Models.User.TrustFactor newTrustFactor = tfl.Add(telegramUserId, -2);
                if(newTrustFactor == null)
                {
                    Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "ERROR: Impossible to record Trust Factor for user id {0} !!.",
                        telegramUserId));

                    return;
                }
                CacheData.TrustFactors.Add(telegramUserId, newTrustFactor);
            }

            CacheData.TrustFactors[telegramUserId].Points += penality;

            Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "Penality added to user id {0} with reason: {1}\n" +
                        "New trust factor: {2}",
                        telegramUserId, action.ToString(),
                        CacheData.TrustFactors[telegramUserId].Points));

            LogTools.AddTrustFactorLog(new TrustFactorLog
            {
                Action = action,
                DateTime =  DateTime.UtcNow,
                TrustFactorId = CacheData.TrustFactors[telegramUserId].TrustFactorId,
                ActionTakenBy = Bot.Manager.MyId
            });
        }
        
        public static bool KickIfInBlacklist(Message message)
        {
            if (CacheData.BannedUsers
                .Where(x => x.TelegramUserId == message.From.Id).Count() > 0)
            {
                string author = message.From.Username == null
                    ? message.From.FirstName + " " + message.From.LastName
                    : message.From.Username;

                try
                {
                    Bot.Manager.BotClient.RestrictChatMemberAsync(
                        message.Chat.Id,
                        message.From.Id,
                        new ChatPermissions()
                        {
                            CanSendMessages = false,
                            CanAddWebPagePreviews = false,
                            CanChangeInfo = false,
                            CanInviteUsers = false,
                            CanPinMessages = false,
                            CanSendMediaMessages = false,
                            CanSendOtherMessages = false,
                            CanSendPolls = false
                        }
                    );
                    Bot.Manager.BotClient.KickChatMemberAsync(message.Chat.Id, message.From.Id);

                    Bot.Manager.BotClient.SendTextMessageAsync(
                        chatId: CacheData.ControlChatId,
                        parseMode: ParseMode.Markdown,
                        text: String.Format(
                            "*[Report]*\n" +
                            "User in blacklist removed from chat.\n" +
                            "\nUserId: {0}" +
                            "\nUsername/Name: {1}" +
                            "\nChat: {2}" +
                            "\n\n*hash_code:* #UB{3}-{4}",
                            message.From.Id,
                            author,
                            message.Chat.Title,
                            message.Chat.Id.ToString().Replace("-", ""),
                            Guid.NewGuid())
                    );

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Bot.Manager.BotClient.SendTextMessageAsync(
                        chatId: CacheData.ControlChatId,
                        parseMode: ParseMode.Markdown,
                        text: String.Format(
                            "*[Log]*\n" +
                            "⚠️Error removing blacklisted user from group.\n" +
                            "\nUserId: {0}" +
                            "\nUsername/Name: {1}" +
                            "\nChat: {2}" +
                            "\nChatId: {3}" +
                            "\n\n*hash_code:* #UB{4}-{5}",
                            message.From.Id,
                            author,
                            message.Chat.Title,
                            message.Chat.Id,
                            message.Chat.Id.ToString().Replace("-", ""),
                            Guid.NewGuid())
                    );
                    return false;
                }
            }

            return false;
        }
        public static bool KickIfIsInBlacklist(Message message, User member)
        {
            if (CacheData.BannedUsers
                .Where(x => x.TelegramUserId == member.Id).Count() > 0)
            {
                string author = member.Username == null
                            ? member.FirstName + " " + member.LastName
                            : member.Username;
                        
                        try
                        {
                            Bot.Manager.BotClient.RestrictChatMemberAsync(
                                    message.Chat.Id,
                                    member.Id,
                                    new ChatPermissions()
                                    {
                                        CanSendMessages = false,
                                        CanAddWebPagePreviews = false,
                                        CanChangeInfo = false,
                                        CanInviteUsers = false,
                                        CanPinMessages = false,
                                        CanSendMediaMessages = false,
                                        CanSendOtherMessages = false,
                                        CanSendPolls = false
                                    }
                                );
                            Bot.Manager.BotClient.KickChatMemberAsync(message.Chat.Id, member.Id);
                            
                            Bot.Manager.BotClient.SendTextMessageAsync(
                                chatId: CacheData.ControlChatId,
                                parseMode: ParseMode.Markdown,
                                text: String.Format(
                                    "*[Report]*\n" +
                                    "User in blacklist removed from chat.\n" +
                                    "\nUserId: {0}" +
                                    "\nUsername/Name: {1}" +
                                    "\nChat: {2}" +
                                    "\n\n*hash_code:* #UB{3}-{4}",
                                    member.Id,
                                    author,
                                    message.Chat.Title,
                                    message.Chat.Id.ToString().Replace("-",""),
                                    Guid.NewGuid())
                            );
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Bot.Manager.BotClient.SendTextMessageAsync(
                                chatId: CacheData.ControlChatId,
                                parseMode: ParseMode.Markdown,
                                text: String.Format(
                                    "*[Log]*\n" +
                                    "⚠️Error removing blacklisted user from group.\n" +
                                    "\nUserId: {0}" +
                                    "\nUsername/Name: {1}" +
                                    "\nChat: {2}" +
                                    "\nChatId: {3}" +
                                    "\n\n*hash_code:* #UB{4}-{5}",
                                    member.Id,
                                    author,
                                    message.Chat.Title,
                                    message.Chat.Id,
                                    message.Chat.Id.ToString().Replace("-",""),
                                    Guid.NewGuid())
                            );
                        }
            }

            return false;
        }
    }
}
