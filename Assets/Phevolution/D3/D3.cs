using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Phevolution
{
    public partial class D3
    {
        public static int min(int[] d) { return d.Min(); }
        // TODO Func selector 到底是什么意思？
        public static int min(int[] d, Func<int, int> selector) { return selector != null ? d.Min(selector) : d.Min(); }
        //public static int scan(int[] d, Func<int, int> selector)
        //{
        //    var range = D3.range(d.Length);
        //    return min(range, a => selector(range[a]));
        //}

        public delegate int Compare(int a, int b);
        public static int scan(int[] values, Compare compare)
        {
            int n = values.Length,
                i = 0, j = 0;
            int xi, xj = values[j];
            while (++i < n)
            {
                if (compare(xi = values[i], xj) < 0 || compare(xj, xj) != 0)
                {
                    xj = xi; j = i;
                }
            }
            if (compare(xj, xj) == 0) return j;
            return -1;
        }

        public delegate int ValueOf(int v, int i, int[] values);
        public static int mean(int[] values, ValueOf valueOf = null)
        {
            int n = values.Length,
                m = n,
                i = -1;
            int value,
                sum = 0;
            if (valueOf == null)
            {
                while (++i < n)
                {
                    value = values[i];
                    sum += value;
                }
            }
            else
            {
                while (++i < n)
                {
                    value = valueOf(values[i], i, values);
                    sum += value;
                }
            }
            return sum / m;
        }
        public static double avg(byte[] d) { return d.Select(t => (int)t).Average(); }
        public static double sum(double[] d) { return d.Sum(); }
        public static double avg(double[] d) { return d.Average(); }

        public static int[] range(int n)
        {
            var ret = new int[n];
            for (var i = 0; i < n; ++i) ret[i] = i;
            return ret;
        }

        public static int[] range(int start, int stop, int step)
        {
            return sequence(start, stop, step);
        }

        public delegate double NormalFunc();
        public delegate NormalFunc NormalBody(double mu, double sigma);
        public static NormalBody randomNormal { get; } = sourceRandomNormal(defaultSourceSet1);

    }
}