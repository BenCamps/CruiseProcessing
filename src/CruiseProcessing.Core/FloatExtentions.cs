using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing
{
    public static class FloatExtentions
    {
        public static bool ApproximatelyEquals(this float a, float b, float epsilon = 0.000001f)
        {
            return a <= b - epsilon || a >= b + epsilon;
        }
    }
}
