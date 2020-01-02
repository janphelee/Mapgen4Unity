using System;
using System.Collections.Generic;

namespace Phevolution
{
    public partial class Utils
    {
        public static double rn(double v, int d = 0)
        {
            var m = Math.Pow(10, d);
            // C#中的Math.Round()默认并不是使用的"四舍五入"法。
            return Math.Round(v * m, MidpointRounding.AwayFromZero) / m;
        }

        // get number from string in format "1-3" or "2" or "0.5"
        public static double getNumberInRange(string s)
        {
            double result;
            if (double.TryParse(s, out result))
            {
                var f = Math.Floor(result);
                return f + (P(result - f) ? 1 : 0);
            }
            //int sign = r[0] == '-' ? -1 : 1;//以负数开始

            var range = s.Contains("-") ? s.Split('-') : null;
            if (range == null || range.Length != 2) { return 0; }

            var count = rand(double.Parse(range[0]), double.Parse(range[1]));
            return count;
        }

        // return value in range [0, 100] (height range)
        public static byte lim(double v)
        {
            return (byte)Math.Max(Math.Min((int)v, 100), 0);
        }

        public static double rand(double min, double max)
        {
            return Math.Floor(Random.NextDouble() * (max - min + 1)) + min;
        }

        // probability shorthand
        public static bool P(double probability)
        {
            return Random.NextDouble() < probability;
        }

        // random number (normal or gaussian distribution)
        public static double gauss(int expected = 100, int deviation = 30, int min = 0, int max = 300, int round = 0)
        {
            return rn(Math.Max(Math.Min(D3.randomNormal(expected, deviation)(), max), min), round);
        }

        public static T pop<T>(List<T> d)
        {
            var ret = d[d.Count - 1];
            d.RemoveAt(d.Count - 1);
            return ret;
        }
    }
}