using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CruiseProcessing
{
    public static class NumberExtensions
    {
        private static double[] roundPower10Double = new double[16]
        {
                1.0, 10.0, 100.0, 1000.0, 10000.0, 100000.0, 1000000.0, 10000000.0, 100000000.0, 1000000000.0,
                10000000000.0, 100000000000.0, 1000000000000.0, 10000000000000.0, 100000000000000.0, 1E+15
        };

        private static float[] roundPower10Single = new float[16]
        {
                1.0f, 10.0f, 100.0f, 1000.0f, 10000.0f, 100000.0f, 1000000.0f, 10000000.0f, 100000000.0f, 1000000000.0f,
                10000000000.0f, 100000000000.0f, 1000000000000.0f, 10000000000000.0f, 100000000000000.0f, 1E+15f
        };


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



        public static bool IsApproximatelyEqual(this float @this, float right, int precision = 3)
        {
            if (precision == 0)
            { return ((int)@this) == (int)right; }

            if (precision < 0 || precision > 15) throw new ArgumentOutOfRangeException(nameof(precision));
            float epslion = 1.0f / roundPower10Single[precision];
            return Math.Abs(@this - right) < epslion;
        }

        public static bool IsApproximatelyEqual(this double @this, double right, int precision = 3)
        {
            if (precision == 0)
            { return ((int)@this) == (int)right; }

            if (precision < 0 || precision > 15) throw new ArgumentOutOfRangeException(nameof(precision));
            double epslion = 1.0d / roundPower10Double[precision];
            return Math.Abs(@this - right) < epslion;
        }


        public static bool IsApproximatelyZero(this float @this, int precision = 3)
        {
            return IsApproximatelyEqual(@this, 0.0f, precision);
        }

        public static bool IsApproximatelyZero(this double @this, int precision = 3)
        {
            return IsApproximatelyEqual(@this, 0.0d, precision);
        }

        public static bool IsExactlyZero(this float @this)
        {
            return @this.Equals(0.0f);
        }

        public static bool IsExactlyZero(this double @this)
        {
            return @this.Equals(0.0d);
        }



        public static bool IsHeightEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 0);
        }

        public static bool IsBdFtEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 0);
        }

        public static bool IsCordsEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 0);
        }

        public static bool IsLbsEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 0);
        }

        public static bool IsPlotSizeEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 0);
        }

        public static bool IsDiameterEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 1);
        }

        public static bool IsStumpHeightEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 1);
        }

        public static bool IsCuFtEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 1);
        }

        public static bool IsKpiEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 1);
        }

        public static bool IsKpiEqual(this double @this, double right)
        {
            return @this.IsApproximatelyEqual(right, 1);
        }

        public static bool IsAcresEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 2);
        }

        public static bool IsAcresEqual(this double @this, double right)
        {
            return @this.IsApproximatelyEqual(right, 2);
        }

        public static bool IsBafEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 2);
        }

        public static bool IsCcfEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 3);
        }

        public static bool IsMbfEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 3);
        }

        public static bool IsTonsEqual(this float @this, float right)
        {
            return @this.IsApproximatelyEqual(right, 3);
        }
    }
}
