using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.MapJobs
{
    using Float = Double;
    using Float2 = double2;

    class PreNoise
    {
        public NativeArray<Float> t_noise0 { get; set; }
        public NativeArray<Float> t_noise1 { get; set; }
        public NativeArray<Float> t_noise2 { get; set; }
        public NativeArray<Float> t_noise3 { get; set; }
        public NativeArray<Float> t_noise4 { get; set; }
        public NativeArray<Float> t_noise5 { get; set; }
        public NativeArray<Float> t_noise6 { get; set; }

        private bool initArray { get; set; }

        private void init(int numTriangles)
        {
            t_noise0 = new NativeArray<Float>(numTriangles, Allocator.Persistent);
            t_noise1 = new NativeArray<Float>(numTriangles, Allocator.Persistent);
            t_noise2 = new NativeArray<Float>(numTriangles, Allocator.Persistent);
            t_noise3 = new NativeArray<Float>(numTriangles, Allocator.Persistent);
            t_noise4 = new NativeArray<Float>(numTriangles, Allocator.Persistent);
            t_noise5 = new NativeArray<Float>(numTriangles, Allocator.Persistent);
            t_noise6 = new NativeArray<Float>(numTriangles, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (initArray)
            {
                t_noise0.Dispose();
                t_noise1.Dispose();
                t_noise2.Dispose();
                t_noise3.Dispose();
                t_noise4.Dispose();
                t_noise5.Dispose();
                t_noise6.Dispose();
            }
        }

        public JobPrecomputedNoise create(SimplexNoise simplex, NativeArray<Float2> _t_vertex)
        {
            var numTriangles = _t_vertex.Length;

            if (!initArray)
            {
                init(numTriangles);
                initArray = true;
            }

            var job = new JobPrecomputedNoise()
            {
                simplex = simplex,
                t_noise0 = t_noise0,
                t_noise1 = t_noise1,
                t_noise2 = t_noise2,
                t_noise3 = t_noise3,
                t_noise4 = t_noise4,
                t_noise5 = t_noise5,
                t_noise6 = t_noise6,
                t_vertex = _t_vertex,
            };
            return job;
        }

        public JobHandle precomputed(SimplexNoise simplex, NativeArray<Float2> _t_vertex, JobHandle other)
        {
            var numTriangles = _t_vertex.Length;

            if (!initArray)
            {
                init(numTriangles);
                initArray = true;
            }

            var job = new JobPrecomputedNoise()
            {
                simplex = simplex,
                t_noise0 = t_noise0,
                t_noise1 = t_noise1,
                t_noise2 = t_noise2,
                t_noise3 = t_noise3,
                t_noise4 = t_noise4,
                t_noise5 = t_noise5,
                t_noise6 = t_noise6,
                t_vertex = _t_vertex,
            };
            return job.Schedule(numTriangles, 64, other);
        }
    }
}
