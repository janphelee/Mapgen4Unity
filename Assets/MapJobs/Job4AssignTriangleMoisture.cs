using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Assets.MapJobs
{
    unsafe struct Job4AssignTriangleMoisture : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction] public int* _triangles;
        [NativeDisableUnsafePtrRestriction] public float* r_rainfall;

        [WriteOnly] public NativeArray<float> t_moisture;

        public void Execute(int index)
        {
            var moisture = 0.0f;
            for (var i = 0; i < 3; i++)
            {
                int s = 3 * index + i,
                    r = s_begin_r(s);
                moisture += r_rainfall[r] / 3;
            }
            t_moisture[index] = moisture;
        }
        private int s_begin_r(int s) { return _triangles[s]; }
    }
}
