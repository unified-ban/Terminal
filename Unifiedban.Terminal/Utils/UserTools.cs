using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Unifiedban.Terminal.Utils
{
    public class UserTools
    {
        private static List<Models.TrustFactorLog> trustFactorLogs =
            new List<Models.TrustFactorLog>();
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
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    text: String.Format(
                        "ERROR: Impossible to record Trust Factor for user id {0} !!.",
                        telegramUserId));

                    return;
                }
                CacheData.TrustFactors.Add(telegramUserId, newTrustFactor);
            }

            CacheData.TrustFactors[telegramUserId].Points += penality;
            trustFactorLogs.Add(new Models.TrustFactorLog
            {
                TrustFactorId = CacheData.TrustFactors[telegramUserId].TrustFactorId,
                Action = action,
                DateTime = DateTime.UtcNow,
                ActionTakenBy = actionTakenBy
            });

            Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    text: String.Format(
                        "Penality added to user id {0} with reason: {1}\n" +
                        "New trust factor: {3}",
                        telegramUserId, action.ToString(),
                        CacheData.TrustFactors[telegramUserId].Points));
        }
    }
}
