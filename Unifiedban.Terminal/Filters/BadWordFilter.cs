/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Unifiedban.Models.Filters;

namespace Unifiedban.Terminal.Filters
{
    public class BadWordFilter : IFilter
    {
        public BadWordFilter()
        {
            if(replacements.Count == 0)
                BuildDictionary();
        }

        static Dictionary<char, string> replacements = new Dictionary<char, string>();
        static BusinessLogic.Filters.BadWordLogic bwl =
                new BusinessLogic.Filters.BadWordLogic();

        public FilterResult DoCheck(Message message)
        {
            if (Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return new FilterResult()
                {
                    CheckName = "BadWord",
                    Result = IFilter.FilterResultType.skipped
                };
            }
            return DoCheck(message, message.Text);
        }
        public FilterResult DoCheck(Message message, string text)
        {
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "BadWordFilter")
                .SingleOrDefault();
            if (configValue != null)
                if (configValue.Value == "false")
                    return new FilterResult()
                    {
                        CheckName = "BadWord",
                        Result = IFilter.FilterResultType.skipped
                    };

            List<BadWord> badWords =
                CacheData.BadWords
                .Where(x => x.GroupId == null || x.GroupId == CacheData.Groups[message.Chat.Id].GroupId)
                .ToList();

            foreach (BadWord badWord in badWords)
            {
                Regex reg = new Regex(badWord.Regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                MatchCollection matchedWords = reg.Matches(text);
                if (matchedWords.Count > 0)
                    return new FilterResult()
                    {
                        CheckName = "BadWord",
                        Result = IFilter.FilterResultType.positive,
                        Rule = badWord.Name
                    };
            }

            return new FilterResult()
            {
                CheckName = "BadWord",
                Result = IFilter.FilterResultType.negative
            };
        }

        public static string BuildRegex(string text)
        {
            string regex = "(";
            char[] charsToReplace = text.ToCharArray();
            foreach (char charToReplace in charsToReplace)
            {
                if (replacements.ContainsKey(charToReplace))
                    regex += replacements[charToReplace];
                else
                    regex += BuildBasicRegexForLetter(charToReplace);
            }
            regex += ")";

            return regex;
        }

        private static void BuildDictionary()
        {
            replacements.Add('A', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('À', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('Á', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('Â', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('Ä', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('Æ', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('Ã', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('Å', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('Ā', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('a', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('à', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('á', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('â', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('ä', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('æ', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('ã', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('å', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('ā', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('ª', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('@', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");
            replacements.Add('4', "[AÀÁÂÄÆÃÅĀaàáâäæãåāª@4]\\s*");

            replacements.Add('B', "[Bbß8]\\s*");
            replacements.Add('b', "[Bbß8]\\s*");
            replacements.Add('ß', "[BbßSŚŠ$sśš58]\\s*");
            replacements.Add('8', "[Bbß8]\\s*");

            replacements.Add('C', "[CÇĆČcçćč([{]\\s*");
            replacements.Add('Ç', "[CÇĆČcçćč([{]\\s*");
            replacements.Add('Ć', "[CÇĆČcçćč([{]\\s*");
            replacements.Add('Č', "[CÇĆČcçćč([{]\\s*");
            replacements.Add('c', "[CÇĆČcçćč([{]\\s*");
            replacements.Add('ç', "[CÇĆČcçćč([{]\\s*");
            replacements.Add('ć', "[CÇĆČcçćč([{]\\s*");
            replacements.Add('č', "[CÇĆČcçćč([{]\\s*");
            replacements.Add('(', "[CÇĆČcçćč([{]\\s*");
            replacements.Add('[', "[CÇĆČcçćč([{]\\s*");
            replacements.Add('{', "[CÇĆČcçćč([{]\\s*");

            replacements.Add('E', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('È', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('É', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('Ê', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('Ë', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('Ę', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('Ė', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('Ē', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('e', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('è', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('é', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('ê', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('ë', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('ę', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('ė', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('ē', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('&', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('€', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");
            replacements.Add('3', "[EÈÉÊËĘĖĒeèéêëęėē&€3]\\s*");

            replacements.Add('I', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('Ì', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('Í', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('Î', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('Ï', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('Į', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('Ī', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('i', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('ì', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('í', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('î', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('ï', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('į', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('ī', "[IÌÍÎÏĮĪiìíîïįī1|!]\\s*");
            replacements.Add('1', "[IÌÍÎÏĮĪiìíîïįī1|Ll!]\\s*");
            replacements.Add('|', "[IÌÍÎÏĮĪiìíîïįī1|Ll!]\\s*");
            replacements.Add('!', "[IÌÍÎÏĮĪiìíîïįī1|Ll!]\\s*");

            replacements.Add('L', "[Ll1|£]\\s*");
            replacements.Add('l', "[Ll1|£]\\s*");
            replacements.Add('£', "[Ll1|£]\\s*");

            replacements.Add('N', "[NÑnñ]\\s*");
            replacements.Add('Ñ', "[NÑnñ]\\s*");
            replacements.Add('n', "[NÑnñ]\\s*");
            replacements.Add('ñ', "[NÑnñ]\\s*");

            replacements.Add('O', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('Ò', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('Ó', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('Ô', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('Ö', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('Õ', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('Œ', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('Ø', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('Ō', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('o', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('ò', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('ó', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('ô', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('ö', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('õ', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('œ', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('ø', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('ō', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('º', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");
            replacements.Add('0', "[OÒÓÔÖÕŒØŌoòóôöõœøōº0]\\s*");

            replacements.Add('S', "[SŚŠ$sßśš5]\\s*");
            replacements.Add('Ś', "[SŚŠ$sßśš5]\\s*");
            replacements.Add('Š', "[SŚŠ$sßśš5]\\s*");
            replacements.Add('$', "[SŚŠ$sßśš5]\\s*");
            replacements.Add('s', "[SŚŠ$sßśš5]\\s*");
            replacements.Add('ś', "[SŚŠ$sßśš5]\\s*");
            replacements.Add('š', "[SŚŠ$sßśš5]\\s*");
            replacements.Add('5', "[SŚŠ$sßśš5]\\s*");

            replacements.Add('t', "[Tt7]\\s*");
            replacements.Add('T', "[Tt7]\\s*");

            replacements.Add('U', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('Ù', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('Ú', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('Û', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('Ü', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('Ū', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('u', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('ù', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('ú', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('û', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('ü', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('ū', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");

            replacements.Add('V', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('v', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");
            replacements.Add('¥', "[UÙÚÛÜŪuùúûüūVv¥]\\s*");

            replacements.Add('Z', "[Zz72]\\s*");
            replacements.Add('z', "[Zz72]\\s*");
            replacements.Add('7', "[Zz72]\\s*");
            replacements.Add('2', "[Zz72]\\s*");
        }

        private static string BuildBasicRegexForLetter(char letter)
        {
            string filteredLetter = letter.ToString()
                .Replace("/", @"\/")
                .Replace(".", @"\.")
                .Replace("?", @"\?")
                .Replace("[", @"\[")
                .Replace("]", @"\]")
                .Replace("*", @"\*");
            
            if (filteredLetter.ToLowerInvariant() == filteredLetter.ToUpperInvariant())
            {
                return "[" + filteredLetter + @"]\s*";
            }
            return "[" + filteredLetter.ToLowerInvariant()
                       + filteredLetter.ToUpperInvariant() + @"]\s*";
        }

        public static bool BanWord(
            string telegramGroup,
            string name,
            string text)
        {
            List<string> parts = new List<string>();

            string[] words = text.Split(" ");
            string regex = "";
            foreach (string word in words)
            {
                string part = BuildRegex(word);
                part += ".*";
                parts.Add(part);

                regex += part;
            }

            regex += "|";
            parts.Reverse();
            foreach (string part in parts)
            {
                regex += part;
            }

            regex = regex.Remove(regex.Length - 2, 2); // remove last .*

            BadWord badWord = bwl.Add(telegramGroup, name, regex, BadWord.State.Active, -2);
            CacheData.BadWords = bwl.Get();

            return badWord == null ? false : true;
        }

        public static bool RemoveBadWord(
            string telegramGroup,
            string name)
        {
            Models.SystemLog.ErrorCodes removed = bwl.Remove(telegramGroup, name, -2);
            CacheData.BadWords = bwl.Get();

            return removed == Models.SystemLog.ErrorCodes.Error ? false : true;
        }
    }
}
