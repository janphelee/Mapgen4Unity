using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Assets.MapJobs
{
    unsafe struct Job3AssignRegionElevation : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction] public int* _r_in_s;
        [NativeDisableUnsafePtrRestriction] public int* _halfedges;
        [NativeDisableUnsafePtrRestriction] public float* t_elevation;

        [WriteOnly] public NativeArray<float> r_elevation;

        public void Execute(int index)
        {
            var count = 0;
            float e = 0;
            bool water = false;
            var s0 = _r_in_s[index];
            var incoming = s0;
            do
            {
                var t = incoming / 3;
                e += t_elevation[t];
                water = water || t_elevation[t] < 0;
                var outgoing = s_next_s(incoming);
                incoming = _halfedges[outgoing];
                count++;
            } while (incoming != s0);
            e /= count;
            if (water && e > 0) { e = -0.001f; }
            r_elevation[index] = e;
        }
        private int s_next_s(int s) { return (s % 3 == 2) ? s - 2 : s + 1; }
    }
}
