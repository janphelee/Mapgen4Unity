using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

    unsafe struct Job2AssignSolidTriangle : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction] public Float* elevation;
        public int paintSize;
        public Float noisy_coastlines;

        [ReadOnly] public NativeArray<Float> t_noise4;
        [ReadOnly] public NativeArray<Float> t_noise5;
        [ReadOnly] public NativeArray<Float> t_noise6;
        [ReadOnly] public NativeArray<Float2> t_vertex;

        [WriteOnly] public NativeArray<Float> t_elevation;

        public void Execute(int index)
        {
            var e = constraintAt(vertex_x(index) / 1000, vertex_y(index) / 1000);
            // TODO: e*e*e*e seems too steep for this, as I want this
            // to apply mostly at the original coastlines and not
            // elsewhere
            t_elevation[index] = (float)(e + noisy_coastlines * (1 - e * e * e * e) * (t_noise4[index] + t_noise5[index] / 2 + t_noise6[index] / 4));
        }

        private Float vertex_x(int i) { return t_vertex[i][0]; }
        private Float vertex_y(int i) { return t_vertex[i][1]; }

        private Float constraintAt(Float x, Float y)
        {
            // https://en.wikipedia.org/wiki/Bilinear_interpolation
            x *= paintSize; y *= paintSize;

            int xInt = (int)Math.Floor(x), yInt = (int)Math.Floor(y);
            Float xFrac = x - xInt, yFrac = y - yInt;

            if (0 <= xInt && xInt + 1 < paintSize && 0 <= yInt && yInt + 1 < paintSize)
            {
                int p = paintSize * yInt + xInt;
                Float
                    e00 = elevation[p],
                    e01 = elevation[p + 1],
                    e10 = elevation[p + paintSize],
                    e11 = elevation[p + paintSize + 1];
                return ((e00 * (1 - xFrac) + e01 * xFrac) * (1 - yFrac)
                        + (e10 * (1 - xFrac) + e11 * xFrac) * yFrac);
            }
            else
            {
                return -1;
            }
        }
    }
}
