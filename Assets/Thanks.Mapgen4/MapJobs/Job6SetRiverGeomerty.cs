using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

#if Use_Double_Float
using Float = System.Double;
using Float2 = Unity.Mathematics.double2;
#else
using Float = System.Single;
using Float2 = Unity.Mathematics.float2;
#endif

namespace Assets.MapJobs
{

    unsafe struct Job6SetRiverGeomerty : IJobParallelFor
    {
        public int numRiverSizes;
        public Float MIN_FLOW;
        public Float RIVER_WIDTH;
        public Float spacing;

        [NativeDisableUnsafePtrRestriction] public int* _halfedges;
        [NativeDisableUnsafePtrRestriction] public int* _triangles;
        [NativeDisableUnsafePtrRestriction] public Float2* _r_vertex;
        [NativeDisableUnsafePtrRestriction] public Float* s_flow;
        [NativeDisableUnsafePtrRestriction] public Float* s_length;
        [NativeDisableUnsafePtrRestriction] public int* flow_out_s;
        [NativeDisableUnsafePtrRestriction] public int* t_downslope_s;

        [NativeDisableUnsafePtrRestriction] public Vector2* river_uv;

        [NativeDisableUnsafePtrRestriction] public Vector3* vertex;
        [NativeDisableUnsafePtrRestriction] public Vector2* uv;

        public void Execute(int index)
        {
            var in9_s = flow_out_s[index];

            var t = in9_s / 3;

            var out_s = t_downslope_s[t];
            var out_flow = s_flow[out_s];

            var row = riverSize(out_s, out_flow);

            var in9_flow = s_flow[s_opposite_s(in9_s)];
            var col = riverSize(in9_s, in9_flow);

            var in1_s = s_next_s(out_s);
            var in2_s = s_next_s(in1_s);
            if (in1_s == in9_s)
            {
                setXYUV(index, out_s, in1_s, in2_s, row, col, 0, 2, 1);
            }
            if (in2_s == in9_s)
            {
                setXYUV(index, out_s, in1_s, in2_s, row, col, 2, 1, 0);
            }
        }

        private int riverSize(int s, Float flow)
        {
            // TODO: performance: build a table of flow to width
            if (s < 0) { return 1; }
            var width = Math.Sqrt(flow - MIN_FLOW) * spacing * RIVER_WIDTH;
            var size = Math.Ceiling(width * numRiverSizes / s_length[s]);
            return Mathf.Clamp((int)size, 1, numRiverSizes);
        }

        void setXYUV(int index, int out_s, int in1_s, int in2_s, int row, int col, int i, int j, int k)
        {
            var t = out_s / 3;

            int r1 = s_begin_r(3 * t),
                r2 = s_begin_r(3 * t + 1),
                r3 = s_begin_r(3 * t + 2);

            vertex[index * 3 + 0] = v3(r1);
            vertex[index * 3 + 1] = v3(r2);
            vertex[index * 3 + 2] = v3(r3);

            var pU = &river_uv[(row * (numRiverSizes + 1) + col) * 6];
            uv[index * 3 + out_s % 3] = pU[i];
            uv[index * 3 + in1_s % 3] = pU[j];
            uv[index * 3 + in2_s % 3] = pU[k];
        }

        private Vector3 v3(int i)
        {
            var v = _r_vertex[i];
            return new Vector3((float)v.x, (float)v.y);
        }

        private int s_next_s(int s) { return (s % 3 == 2) ? s - 2 : s + 1; }
        private int s_opposite_s(int s) { return _halfedges[s]; }
        private int s_begin_r(int s) { return _triangles[s]; }
    }
}
