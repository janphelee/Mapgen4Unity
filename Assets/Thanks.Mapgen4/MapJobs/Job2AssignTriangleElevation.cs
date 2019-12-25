using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

#if Use_Double_Float
using Float = System.Double;
using Float2 = Unity.Mathematics.double2;
#else
using Float = System.Single;
using Float2 = Unity.Mathematics.float2;
#endif

namespace Assets.MapJobs
{

    struct Job2AssignTriangleElevation : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Float> t_noise0;
        [ReadOnly] public NativeArray<Float> t_noise1;
        [ReadOnly] public NativeArray<Float> t_noise2;
        [ReadOnly] public NativeArray<Float> t_noise4;

        [ReadOnly] public NativeArray<Float> t_mountain_distance;

        public Float hill_height;
        public Float ocean_depth;
        public int mountain_slope;

        public NativeArray<Float> t_elevation;

        public void Execute(int index)
        {
            double e = t_elevation[index];
            if (e > 0)
            {
                /* Mix two sources of elevation:
                 *
                 * 1. eh: Hills are formed using simplex noise. These
                 *    are very low amplitude, and the main purpose is
                 *    to make the rivers meander. The amplitude
                 *    doesn't make much difference in the river
                 *    meandering. These hills shouldn't be
                 *    particularly visible so I've kept the amplitude
                 *    low.
                 *
                 * 2. em: Mountains are formed using something similar to
                 *    worley noise. These form distinct peaks, with
                 *    varying distance between them.
                 */
                // TODO: precompute eh, em per triangle
                var noisiness = 1.0 - 0.5 * (1 + t_noise0[index]);
                var eh = (1 + noisiness * t_noise4[index] + (1 - noisiness) * t_noise2[index]) * hill_height;
                if (eh < 0.01) { eh = 0.01; }
                var em = 1 - mountain_slope / 1000.0 * t_mountain_distance[index];
                if (em < 0.01) { em = 0.01; }
                var weight = e * e;
                e = (1.0 - weight) * eh + weight * em;
            }
            else
            {
                /* Add noise to make it more interesting. */
                e *= ocean_depth + t_noise1[index];
            }
            if (e < -1.0) { e = -1.0; }
            if (e > +1.0) { e = +1.0; }
            t_elevation[index] = (float)e;
        }
    }
}
