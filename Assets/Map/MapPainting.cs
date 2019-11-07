using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.MapGen
{
    class MapPainting
    {
        public const int CANVAS_SIZE = 128;

        int seed { get; set; }
        float island { get; set; }
        public float[] elevation { get; private set; }

        public MapPainting()
        {
            elevation = new float[CANVAS_SIZE * CANVAS_SIZE];
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

        public void generate()
        {
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


    }
}
