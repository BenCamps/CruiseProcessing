using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing
{
    public static class NumberExtentions
    {
        public static bool ApproximatelyEquals(this float a, float b, float epsilon = 0.000001f)
        {
            return a <= b - epsilon || a >= b + epsilon;
        }

        public static float Round(this float @this, int digits)
        {
            return (float)Math.Round(@this, digits);
        }

        public static float RoundDiameter(this float @this)
        {
            return (float)Math.Round(@this, 1);
        }

        public static float RoundHeight(this float @this)
        {
            return (float)Math.Round(@this, 2);
        }
    }
}
