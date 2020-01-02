using System;

namespace Phevolution
{
    public partial class D3
    {
        public static double polygonArea(double[][] polygon)
        {
            int i = -1,
                n = polygon.Length;
            double[]
                a,
                b = polygon[n - 1];
            double area = 0;

            while (++i < n)
            {
                a = b;
                b = polygon[i];
                area += a[1] * b[0] - a[0] * b[1];
            }

            return area / 2;
        }

    }
}