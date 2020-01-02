using System;
using Unity.Collections;
using Unity.Jobs;

using Float = System.Single;

namespace Assets.MapJobs
{

    struct Job1ElevationGenerate : IJobParallelFor
    {
        public int size;
        public Float island;
        public SimplexNoise simplex;

        [WriteOnly] public NativeArray<Float> elevation;

        public void Execute(int index)
        {
            var x = index % size;
            var y = index / size;
            float
                nx = 2f * x / size - 1,
                ny = 2f * y / size - 1;
            var distance = Math.Max(Math.Abs(nx), Math.Abs(ny));
            var e = 0.5 * (fbm_noise(nx, ny) + island * (0.75 - 2 * distance * distance));
            if (e < -1.0) { e = -1.0; }
            if (e > +1.0) { e = +1.0; }
            elevation[index] = (Float)e;
            if (e > 0)
            {
                var m = 0.5 * simplex.noise(nx + 30, ny + 50)
                         + 0.5 * simplex.noise(2 * nx + 33, 2 * ny + 55);
                // TODO: make some of these into parameters
                var mountain = Math.Min(1.0, e * 5.0) * (1 - Math.Abs(m) / 0.5);
                if (mountain > 0.0)
                {
                    elevation[index] = (Float)Math.Max(e, Math.Min(e * 3, mountain));
                }
            }
        }

        //var amplitudes = new float[] { 1f, 1f / 2, 1f / 4, 1f / 8, 1f / 16 };
        private float amplitudesAt(int octave)
        {
            return 1f / (1 << octave);
        }
        private double fbm_noise(double nx, double ny)
        {
            double sum = 0, sumOfAmplitudes = 0;
            for (var octave = 0; octave < 5; octave++)
            {
                var frequency = 1 << octave;
                sum += amplitudesAt(octave) * simplex.noise(nx * frequency, ny * frequency);
                sumOfAmplitudes += amplitudesAt(octave);
            }
            return sum / sumOfAmplitudes;
        }
    }
}
