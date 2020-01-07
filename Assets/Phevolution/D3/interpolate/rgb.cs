using System;
using UnityEngine;

namespace Phevolution
{
    public partial class D3
    {
        public static Func<string[], Func<float, Color>> rgbSpline(Func<float[], Func<float, float>> spline)
        {
            return colors =>
            {
                var n = colors.Length;
                var r = new float[n];
                var g = new float[n];
                var b = new float[n];

                Color color = Color.white;

                for (var i = 0; i < n; ++i)
                {
                    color = colors[i].ToColor();
                    //Debug.Log($"{i} {colors[i]} {color}");
                    r[i] = color.r;
                    g[i] = color.g;
                    b[i] = color.b;
                }

                var fr = spline(r);
                var fg = spline(g);
                var fb = spline(b);
                color.a = 1;
                return t =>
                {
                    color.r = fr(t);
                    color.g = fg(t);
                    color.b = fb(t);
                    return color;
                };
            };
        }

        public static readonly Func<string[], Func<float, Color>> rgbBasis = rgbSpline(basis);
    }
}