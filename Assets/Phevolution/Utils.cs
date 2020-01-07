using System;
using System.Collections.Generic;
using UnityEngine;

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

        public static float angle(float x0, float y0, float x1, float y1)
        {
            var p1 = new Vector2(x0, y0).normalized;
            var p2 = new Vector2(x1, y1).normalized;

            var aa = angle(p1, p2);
            var cc = Vector3.Cross(p1, p2).z < 0;
            if (cc) aa = 360 - aa;//反向旋转角度
            return aa;
        }

        public static float angle(Vector2 p1, Vector2 p2)
        {
            var ret = Mathf.Acos(Vector2.Dot(p1, p2)) * Mathf.Rad2Deg;
            //Debug.Log($"angle {p1} {p2} ret:{ret}");
            return ret;
        }

        // Array.Sort 为不稳定排序
        public static void InsertionSort<T>(IList<T> list, Comparison<T> comparison)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            if (comparison == null)
                throw new ArgumentNullException("comparison");

            int count = list.Count;
            for (int j = 1; j < count; j++)
            {
                T key = list[j];

                int i = j - 1;
                for (; i >= 0 && comparison(list[i], key) > 0; i--)
                {
                    list[i + 1] = list[i];
                }
                list[i + 1] = key;
            }
        }
    }
}