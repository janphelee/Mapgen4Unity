using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.MapJobs
{
    using Float = Double;
    using Float2 = double2;

    struct JobPrecomputedNoise : IJobParallelFor
    {
        public SimplexNoise simplex;
        [ReadOnly] public NativeArray<Float2> t_vertex;

        [WriteOnly] public NativeArray<Float> t_noise0;
        [WriteOnly] public NativeArray<Float> t_noise1;
        [WriteOnly] public NativeArray<Float> t_noise2;
        [WriteOnly] public NativeArray<Float> t_noise3;
        [WriteOnly] public NativeArray<Float> t_noise4;
        [WriteOnly] public NativeArray<Float> t_noise5;
        [WriteOnly] public NativeArray<Float> t_noise6;

        public void Execute(int index)
        {
            var nx = (vertex_x(index) - 500) / 500;
            var ny = (vertex_y(index) - 500) / 500;
            t_noise0[index] = simplex.noise(nx, ny);
            t_noise1[index] = simplex.noise(2 * nx + 5, 2 * ny + 5);
            t_noise2[index] = simplex.noise(4 * nx + 7, 4 * ny + 7);
            t_noise3[index] = simplex.noise(8 * nx + 9, 8 * ny + 9);
            t_noise4[index] = simplex.noise(16 * nx + 15, 16 * ny + 15);
            t_noise5[index] = simplex.noise(32 * nx + 31, 32 * ny + 31);
            t_noise6[index] = simplex.noise(64 * nx + 67, 64 * ny + 67);
        }
        private Float vertex_x(int i) { return t_vertex[i][0]; }
        private Float vertex_y(int i) { return t_vertex[i][1]; }
    }
}
