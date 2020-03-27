using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Unifiedban.Terminal.Utils
{
    public class UserTools
    {
        public static bool NameIsRTL(string fullName)
        {
            string regex = @"[\u0591-\u07FF]+";

            Regex reg = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            MatchCollection matchedWords = reg.Matches(fullName);
            if (matchedWords.Count > 0)
                return true;

            return false;
        }
    }
}
