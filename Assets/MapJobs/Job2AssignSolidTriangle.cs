﻿using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.MapJobs
{
    unsafe struct Job2AssignSolidTriangle : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction] public float* elevation;
        public int paintSize;
        public float noisy_coastlines;

        [ReadOnly] public NativeArray<float> t_noise4;
        [ReadOnly] public NativeArray<float> t_noise5;
        [ReadOnly] public NativeArray<float> t_noise6;
        [ReadOnly] public NativeArray<float2> t_vertex;

        [WriteOnly] public NativeArray<float> t_elevation;

        public void Execute(int index)
        {
            var e = constraintAt(vertex_x(index) / 1000, vertex_y(index) / 1000);
            // TODO: e*e*e*e seems too steep for this, as I want this
            // to apply mostly at the original coastlines and not
            // elsewhere
            t_elevation[index] = (float)(e + noisy_coastlines * (1 - e * e * e * e) * (t_noise4[index] + t_noise5[index] / 2 + t_noise6[index] / 4));
        }

        private float vertex_x(int i) { return t_vertex[i][0]; }
        private float vertex_y(int i) { return t_vertex[i][1]; }

        private double constraintAt(float x, float y)
        {
            // https://en.wikipedia.org/wiki/Bilinear_interpolation
            x *= paintSize; y *= paintSize;

            int xInt = (int)Math.Floor(x), yInt = (int)Math.Floor(y);
            float xFrac = x - xInt, yFrac = y - yInt;

            if (0 <= xInt && xInt + 1 < paintSize && 0 <= yInt && yInt + 1 < paintSize)
            {
                int p = paintSize * yInt + xInt;
                double
                    e00 = elevation[p],
                    e01 = elevation[p + 1],
                    e10 = elevation[p + paintSize],
                    e11 = elevation[p + paintSize + 1];
                return ((e00 * (1 - xFrac) + e01 * xFrac) * (1 - yFrac)
                        + (e10 * (1 - xFrac) + e11 * xFrac) * yFrac);
            }
            else
            {
                return -1.0;
            }
        }
    }
}
