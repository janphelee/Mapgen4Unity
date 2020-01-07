using System;

namespace Phevolution
{
    public partial class D3
    {
        private static double __basis(double t1, double v0, double v1, double v2, double v3)
        {
            var t2 = t1 * t1;
            var t3 = t2 * t1;
            return ((1 - 3 * t1 + 3 * t2 - t3) * v0
                + (4 - 6 * t2 + 3 * t3) * v1
                + (1 + 3 * t1 + 3 * t2 - 3 * t3) * v2
                + t3 * v3) / 6;
        }

        public static Func<float, float> basis(float[] values)
        {
            var n = values.Length - 1;
            return (t) =>
            {
                int i;
                if (t <= 0) t = i = 0;
                else if (t >= 1) { t = 1; i = n - 1; }
                else i = (int)Math.Floor(t * n);
                double
                    v1 = values[i],
                    v2 = values[i + 1],
                    v0 = i > 0 ? values[i - 1] : 2 * v1 - v2,
                    v3 = i < n - 1 ? values[i + 2] : 2 * v2 - v1;
                return (float)__basis((t - (float)i / n) * n, v0, v1, v2, v3);
            };
        }
    }
}