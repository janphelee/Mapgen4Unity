using Assets.MapUtil;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Map
{
    class MapPaint
    {
        public const int CANVAS_SIZE = 128;

        private int seed { get; set; }
        private float island { get; set; }

        public float[] elevation { get; private set; }

        /* currentStroke */
        private float[] currentStrokePreviousElevation { get; set; }
        private float[] currentStrokeTime { get; set; }
        private float[] currentStrokeStrength { get; set; }

        public MapPaint()
        {
            elevation = new float[CANVAS_SIZE * CANVAS_SIZE];

            currentStrokePreviousElevation = new float[CANVAS_SIZE * CANVAS_SIZE];
            currentStrokeStrength = new float[CANVAS_SIZE * CANVAS_SIZE];
            currentStrokeTime = new float[CANVAS_SIZE * CANVAS_SIZE];
        }

        public void setElevationParam(int seed = 187, float island = 0.5f)
        {
            if (seed != this.seed || island != this.island)
            {
                this.seed = seed;
                this.island = island;
                this.generate();
            }
        }

        private void generate()
        {
            Debug.Log($"dphe MapPainting.generate seed:{seed} island:{island}");
            var noise = new SimplexNoise(Rander.makeRandFloat(this.seed));
            var amplitudes = new float[] { 1f, 1f / 2, 1f / 4, 1f / 8, 1f / 16 };

            double fbm_noise(double nx, double ny)
            {
                double sum = 0, sumOfAmplitudes = 0;
                for (var octave = 0; octave < amplitudes.Length; octave++)
                {
                    var frequency = 1 << octave;
                    sum += amplitudes[octave] * noise.noise(nx * frequency, ny * frequency);
                    sumOfAmplitudes += amplitudes[octave];
                }
                return sum / sumOfAmplitudes;
            }

            for (var y = 0; y < CANVAS_SIZE; y++)
            {
                for (var x = 0; x < CANVAS_SIZE; x++)
                {
                    var p = y * CANVAS_SIZE + x;
                    float
                        nx = 2f * x / CANVAS_SIZE - 1,
                        ny = 2f * y / CANVAS_SIZE - 1;
                    var distance = Math.Max(Math.Abs(nx), Math.Abs(ny));
                    var e = 0.5 * (fbm_noise(nx, ny) + island * (0.75 - 2 * distance * distance));
                    if (e < -1.0) { e = -1.0; }
                    if (e > +1.0) { e = +1.0; }
                    elevation[p] = (float)e;
                    if (e > 0)
                    {
                        var m = (0.5 * noise.noise(nx + 30, ny + 50)
                                 + 0.5 * noise.noise(2 * nx + 33, 2 * ny + 55));
                        // TODO: make some of these into parameters
                        var mountain = Math.Min(1.0, e * 5.0) * (1 - Math.Abs(m) / 0.5);
                        if (mountain > 0.0)
                        {
                            elevation[p] = (float)Math.Max(e, Math.Min(e * 3, mountain));
                        }
                    }
                }
            }
        }

        public class Size
        {
            public int key { get; set; }
            public int rate { get; set; }
            public int innerRadius { get; set; }
            public int outerRadius { get; set; }
        }
        public readonly Dictionary<string, Size> SIZES = new Dictionary<string, Size>()
        {
            {"小02",  new Size(){ key=1,rate=8,innerRadius=2,outerRadius=6}},
            {"中05", new Size(){ key=2,rate=5,innerRadius=5,outerRadius=10}},
            {"大10",  new Size(){ key=3,rate=3,innerRadius=10,outerRadius=16}},
        };
        public class Tool
        {
            public float elevation { get; set; }
        }
        public readonly Dictionary<string, Tool> TOOLS = new Dictionary<string, Tool>() {
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
        public void paintAt(Tool tool, float x0, float y0, Size size, float deltaTimeInSec)
        {
            var elevation = this.elevation;
            /* This has two effects: first time you click the mouse it has a
             * strong effect, and it also limits the amount in case you
             * pause */
            deltaTimeInSec = Math.Min(0.1f, deltaTimeInSec);

            var newElevation = tool.elevation;
            var innerRadius = size.innerRadius;
            var outerRadius = size.outerRadius;
            var rate = size.rate;
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
            elevation.CopyTo(currentStrokePreviousElevation, 0);

            dragPen(p, s1, s2);
        }

        public void dragPen(Vector2 p, string s1, string s2)
        {
            paintAt(TOOLS[s2], p.x, p.y, SIZES[s1], Time.deltaTime);
        }

        private void reset<T>(T[] a, T b)
        {
            for (int i = 0; i < a.Length; ++i) a[i] = b;
        }
    }
}
