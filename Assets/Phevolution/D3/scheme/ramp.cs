using System;
using System.Linq;
using UnityEngine;

namespace Phevolution
{
    public partial class D3
    {
        public static Func<float, Color> ramp(string[][] scheme)
        {
            return rgbBasis(scheme[scheme.Length - 1]);
        }

        public static Func<float, Color> ramp(string[] scheme)
        {
            var colors = scheme.Select(s => s.ToColor()).ToArray();

            var n = colors.Length - 1;
            var step = 1f / n;

            return t =>
            {
                if (t == 0) return colors[0];
                if (t == 1) return colors[n];

                var idx = (int)Mathf.Floor(t / step);
                var mod = t - t * idx;
                return Color.Lerp(colors[idx], colors[idx + 1], mod / step);
            };
        }
    }
}