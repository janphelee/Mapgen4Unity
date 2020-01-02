using System;

namespace Phevolution
{
    public partial class D3
    {
        private static int[] sequence(int start, int stop, int step)
        {
            var n = Math.Max(0, (int)Math.Ceiling((stop - start) / (double)step));
            var range = new int[n];

            var i = -1;
            while (++i < n)
            {
                range[i] = (start + i * step);
            }

            return range;
        }
        private static NormalBody sourceRandomNormal(NormalFunc source)
        {
            NormalBody randomNormal = (mu, sigma) =>
            {
                double[] x = null, r = null;
                return new NormalFunc(() =>
                {
                    double y;
                    // If available, use the second previously-generated uniform random.
                    if (x != null)
                    {
                        y = x[0];
                        x = null;
                    }
                    // Otherwise, generate a new x and y.
                    else
                    {
                        do
                        {
                            x = new double[] { source() * 2 - 1 };
                            y = source() * 2 - 1;
                            r = new double[] { x[0] * x[0] + y * y };
                        } while (r == null || r[0] > 1);
                    }
                    return mu + sigma * y * Math.Sqrt(-2 * Math.Log(r[0]) / r[0]);
                });
            };

            return randomNormal;
        }

        private static double defaultSourceSet1()
        {
            return Random.NextDouble();
        }


    }
}