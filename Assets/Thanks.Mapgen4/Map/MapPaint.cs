using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

#if Use_Double_Float
using Float = System.Double;
#else
using Float = System.Single;
#endif

namespace Assets.MapJobs
{

    class MapPaint : IDisposable
    {
        public const int CANVAS_SIZE = 128;

        private NativeArray<Float> elevation { get; set; }
        /* currentStroke */
        private Float[] currentStrokePreviousElevation { get; set; }
        private Float[] currentStrokeTime { get; set; }
        private Float[] currentStrokeStrength { get; set; }

        public MapPaint(NativeArray<Float> elevation)
        {
            this.elevation = elevation;

            var size = elevation.Length;
            currentStrokePreviousElevation = new Float[size];
            currentStrokeStrength = new Float[size];
            currentStrokeTime = new Float[size];
        }

        public class Size
        {
            public int key { get; set; }
            public int rate { get; set; }
            public int innerRadius { get; set; }
            public int outerRadius { get; set; }
        }
        public static readonly Dictionary<string, Size> SIZES = new Dictionary<string, Size>()
        {
            {"小02",  new Size(){ key=1,rate=8,innerRadius=2,outerRadius=6}},
            {"中05", new Size(){ key=2,rate=5,innerRadius=5,outerRadius=10}},
            {"大10",  new Size(){ key=3,rate=3,innerRadius=10,outerRadius=16}},
        };
        public class Tool
        {
            public float elevation { get; set; }
        }
        public static readonly Dictionary<string, Tool> TOOLS = new Dictionary<string, Tool>() {
            {"海洋", new Tool(){elevation=-0.25f}},
            {"湖水", new Tool(){elevation=-0.05f}},
            {"平原", new Tool(){elevation=+0.05f}},
            {"山峰", new Tool(){elevation=+1.00f}}
        };

        /**
         * Paint a circular region
         *
         * @param {{elevation: number}} tool
         * @param {number} x0 - should be 0 to 1
         * @param {number} y0 - should be 0 to 1
         * @param {{innerRadius: number, outerRadius: number, rate: number}} size
         * @param {number} deltaTimeInSec
         */
        public void paintAt(float x0, float y0, int rate, int innerRadius, int outerRadius, float newElevation, float deltaTimeInSec)
        {
            var elevation = this.elevation;
            /* This has two effects: first time you click the mouse it has a
             * strong effect, and it also limits the amount in case you
             * pause */
            deltaTimeInSec = Math.Min(0.1f, deltaTimeInSec);

            int xc = (int)(x0 * CANVAS_SIZE), yc = (int)(y0 * CANVAS_SIZE);
            int top = Math.Max(0, yc - outerRadius),
                bottom = Math.Min(CANVAS_SIZE - 1, yc + outerRadius);
            for (var y = top; y <= bottom; y++)
            {
                int s = (int)Math.Sqrt(outerRadius * outerRadius - (y - yc) * (y - yc));
                int left = Math.Max(0, xc - s),
                    right = Math.Min(CANVAS_SIZE - 1, xc + s);
                for (var x = left; x <= right; x++)
                {
                    var p = y * CANVAS_SIZE + x;
                    var distance = Math.Sqrt((x - xc) * (x - xc) + (y - yc) * (y - yc));
                    var strength = 1.0f - (float)Math.Min(1, Math.Max(0, (distance - innerRadius) / (outerRadius - innerRadius)));
                    var factor = rate * deltaTimeInSec;
                    currentStrokeTime[p] += strength * factor;
                    if (strength > currentStrokeStrength[p])
                    {
                        currentStrokeStrength[p] = (1f - factor) * currentStrokeStrength[p] + factor * strength;
                    }
                    var mix = currentStrokeStrength[p] * Math.Min(1f, currentStrokeTime[p]);
                    elevation[p] = (1f - mix) * currentStrokePreviousElevation[p] + mix * newElevation;
                }
            }
        }

        public void startPen(Vector2 p, string s1, string s2)
        {
            reset(currentStrokeTime, 0);
            reset(currentStrokeStrength, 0);
            elevation.CopyTo(currentStrokePreviousElevation);

            dragPen(p, s1, s2);
        }

        public void dragPen(Vector2 p, string s1, string s2)
        {
            var size = SIZES[s1];
            var tool = TOOLS[s2];
            paintAt(p.x, p.y, size.rate, size.innerRadius, size.outerRadius, tool.elevation, Time.deltaTime);
        }

        private void reset<T>(T[] a, T b)
        {
            for (int i = 0; i < a.Length; ++i) a[i] = b;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
