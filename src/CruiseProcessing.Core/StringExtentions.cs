using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace CruiseProcessing
{
    public static class StringExtentions
    {
        public static string Limit(this string str, int maxLengeth)
        {
            if(maxLengeth < 0) { throw new ArgumentOutOfRangeException(nameof(maxLengeth)); }

            var length = Math.Min(maxLengeth, str.Length);
            return str.Substring(0, length);
        }

        public static string LimitAndPadRight(this string str, int length, char padChar)
        {
            str = Limit(str, length);
            return str.PadRight(length, padChar);

        }
    }
}
