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
        static Dictionary<char, string> replacements = new Dictionary<char, string>();

        public FilterResult DoCheck(Message message)
        {
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

            List<Models.Filters.BadWord> badWords =
                CacheData.BadWords
                .Where(x => x.GroupId == null || x.GroupId == CacheData.Groups[message.Chat.Id].GroupId)
                .ToList();

            foreach (Models.Filters.BadWord badWord in badWords)
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
            }
            regex += ")";

            return regex;
        }

        public static void BuildDictionary()
        {
            replacements.Add('A', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('À', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('Á', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('Â', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('Ä', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('Æ', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('Ã', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('Å', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('Ā', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('a', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('à', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('á', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('â', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('ä', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('æ', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('ã', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('å', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('ā', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('ª', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('@', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");
            replacements.Add('4', "[A*À*Á*Â*Ä*Æ*Ã*Å*Ā*a*à*á*â*ä*æ*ã*å*ā*ª*@*4*]\\s*");

            replacements.Add('B', "[B*b*ß*8*]\\s*");
            replacements.Add('b', "[B*b*ß*8*]\\s*");
            replacements.Add('ß', "[B*b*ß*S*Ś*Š*$*s*ś*š*5*8*]\\s*");
            replacements.Add('8', "[B*b*ß*8*]\\s*");

            replacements.Add('C', "[C*Ç*Ć*Č*c*ç*ć*č*(*[*{*]\\s*");
            replacements.Add('Ç', "[C*Ç*Ć*Č*c*ç*ć*č*(*[*{*]\\s*");
            replacements.Add('Ć', "[C*Ç*Ć*Č*c*ç*ć*č*(*[*{*]\\s*");
            replacements.Add('Č', "[C*Ç*Ć*Č*c*ç*ć*č*(*[*{*]\\s*");
            replacements.Add('c', "[C*Ç*Ć*Č*c*ç*ć*č*(*[*{*]\\s*");
            replacements.Add('ç', "[C*Ç*Ć*Č*c*ç*ć*č*(*[*{*]\\s*");
            replacements.Add('ć', "[C*Ç*Ć*Č*c*ç*ć*č*(*[*{*]\\s*");
            replacements.Add('č', "[C*Ç*Ć*Č*c*ç*ć*č*(*[*{*]\\s*");
            replacements.Add('(', "[C*Ç*Ć*Č*c*ç*ć*č*(*[*{*]\\s*");
            replacements.Add('[', "[C*Ç*Ć*Č*c*ç*ć*č*(*[*{*]\\s*");
            replacements.Add('{', "[C*Ç*Ć*Č*c*ç*ć*č*(*[*{*]\\s*");

            replacements.Add('E', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('È', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('É', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('Ê', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('Ë', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('Ę', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('Ė', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('Ē', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('e', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('è', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('é', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('ê', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('ë', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('ę', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('ė', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('ē', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('&', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('€', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");
            replacements.Add('3', "[E*È*É*Ê*Ë*Ę*Ė*Ē*e*è*é*ê*ë*ę*ė*ē*&*€*3*]\\s*");

            replacements.Add('I', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('Ì', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('Í', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('Î', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('Ï', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('Į', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('Ī', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('i', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('ì', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('í', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('î', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('ï', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('į', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('ī', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*]\\s*");
            replacements.Add('1', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*L*l*]\\s*");
            replacements.Add('|', "[I*Ì*Í*Î*Ï*Į*Ī*i*ì*í*î*ï*į*ī*1*|*L*l*]\\s*");

            replacements.Add('L', "[L*l*1*|*£*]\\s*");
            replacements.Add('l', "[L*l*1*|*£*]\\s*");
            replacements.Add('£', "[L*l*1*|*£*]\\s*");

            replacements.Add('N', "[N*Ñ*n*ñ*]\\s*");
            replacements.Add('Ñ', "[N*Ñ*n*ñ*]\\s*");
            replacements.Add('n', "[N*Ñ*n*ñ*]\\s*");
            replacements.Add('ñ', "[N*Ñ*n*ñ*]\\s*");

            replacements.Add('O', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('Ò', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('Ó', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('Ô', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('Ö', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('Õ', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('Œ', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('Ø', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('Ō', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('o', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('ò', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('ó', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('ô', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('ö', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('õ', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('œ', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('ø', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('ō', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('º', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");
            replacements.Add('0', "[O*Ò*Ó*Ô*Ö*Õ*Œ*Ø*Ō*o*ò*ó*ô*ö*õ*œ*ø*ō*º*0*]\\s*");

            replacements.Add('S', "[S*Ś*Š*$*s*ß*ś*š*5*]\\s*");
            replacements.Add('Ś', "[S*Ś*Š*$*s*ß*ś*š*5*]\\s*");
            replacements.Add('Š', "[S*Ś*Š*$*s*ß*ś*š*5*]\\s*");
            replacements.Add('$', "[S*Ś*Š*$*s*ß*ś*š*5*]\\s*");
            replacements.Add('s', "[S*Ś*Š*$*s*ß*ś*š*5*]\\s*");
            replacements.Add('ś', "[S*Ś*Š*$*s*ß*ś*š*5*]\\s*");
            replacements.Add('š', "[S*Ś*Š*$*s*ß*ś*š*5*]\\s*");
            replacements.Add('5', "[S*Ś*Š*$*s*ß*ś*š*5*]\\s*");

            replacements.Add('t', "[T*t*7*]\\s*");
            replacements.Add('T', "[T*t*7*]\\s*");

            replacements.Add('U', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('Ù', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('Ú', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('Û', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('Ü', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('Ū', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('u', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('ù', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('ú', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('û', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('ü', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('ū', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");

            replacements.Add('V', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('v', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");
            replacements.Add('¥', "[U*Ù*Ú*Û*Ü*Ū*u*ù*ú*û*ü*ū*V*v*¥*]\\s*");

            replacements.Add('Z', "[Z*z*7*2*]\\s*");
            replacements.Add('z', "[Z*z*7*2*]\\s*");
            replacements.Add('7', "[Z*z*7*2*]\\s*");
            replacements.Add('2', "[Z*z*7*2*]\\s*");
        }

        public static bool BanWord(
            string telegramGroup,
            string name,
            string text)
        {
            string[] words = text.Split(" ");
            string regex = "";
            foreach (string word in words)
            {
                regex += BuildRegex(word);
                regex += ".*";
            }
            regex = regex.Remove(regex.Length - 2, 2); // remove last .*
            
            BusinessLogic.Filters.BadWordLogic bwl =
                new BusinessLogic.Filters.BadWordLogic();
            BadWord badWord = bwl.Add(telegramGroup, name, regex, Models.Filters.BadWord.State.Active, -2);

            return badWord == null ? false : true;
        }
    }
}
