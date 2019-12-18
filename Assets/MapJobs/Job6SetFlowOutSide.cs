using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.MapJobs
{
    using Float = Double;
    using Float2 = double2;

    unsafe struct Job6SetFlowOutSide : IJob
    {
        public float MIN_FLOW;
        public int numSolidTriangles;

        [NativeDisableUnsafePtrRestriction] public int* t_downslope_s;

        [ReadOnly] public NativeArray<int> _halfedges;
        [ReadOnly] public NativeArray<Float> s_flow;

        [WriteOnly] public NativeArray<int> flow_out_s;
        [WriteOnly] public NativeArray<int> result;

        public void Execute()
        {
            var count = 0;
            for (int t = 0; t < numSolidTriangles; ++t)
            {
                var out_s = t_downslope_s[t];
                if (out_s < 0) continue;

                var out_flow = s_flow[out_s];//@array
                if (out_flow < MIN_FLOW) continue;

                int in1_s = s_next_s(out_s);
                int in2_s = s_next_s(in1_s);
                var in1_flow = s_flow[s_opposite_s(in1_s)];
                var in2_flow = s_flow[s_opposite_s(in2_s)];

                if (in1_flow >= MIN_FLOW) flow_out_s[count++] = in1_s;
                if (in2_flow >= MIN_FLOW) flow_out_s[count++] = in2_s;
            }
            result[0] = count;
        }

        private int s_next_s(int s) { return (s % 3 == 2) ? s - 2 : s + 1; }
        private int s_opposite_s(int s) { return _halfedges[s]; }
    }
}
