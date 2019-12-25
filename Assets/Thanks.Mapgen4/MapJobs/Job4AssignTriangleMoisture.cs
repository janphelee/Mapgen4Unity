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

    unsafe struct Job4AssignTriangleMoisture : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction] public int* _triangles;
        [NativeDisableUnsafePtrRestriction] public Float* r_rainfall;

        [WriteOnly] public NativeArray<Float> t_moisture;

        public void Execute(int index)
        {
            Float moisture = 0;
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
