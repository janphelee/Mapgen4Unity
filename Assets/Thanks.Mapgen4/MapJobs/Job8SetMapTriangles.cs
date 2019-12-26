using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

#if Use_Double_Float
using Float = System.Double;
#else
using Float = System.Single;
#endif

namespace Assets.MapJobs
{

    unsafe struct Job8SetMapTriangles : IJobParallelFor
    {
        public int numRegions;

        [NativeDisableUnsafePtrRestriction] public int* _triangles;
        [NativeDisableUnsafePtrRestriction] public int* _halfedges;
        [NativeDisableUnsafePtrRestriction] public Float* r_elevation;
        [NativeDisableUnsafePtrRestriction] public Float* s_flow;

        [NativeDisableUnsafePtrRestriction] public int* I;

        public void Execute(int index)
        {
            int opposite_s = s_opposite_s(index),
                r1 = s_begin_r(index),
                r2 = s_begin_r(opposite_s),
                t1 = s_inner_t(index),
                t2 = s_inner_t(opposite_s);

            // Each quadrilateral is turned into two triangles, so each
            // half-edge gets turned into one. There are two ways to fold
            // a quadrilateral. This is usually a nuisance but in this
            // case it's a feature. See the explanation here
            // https://www.redblobgames.com/x/1725-procedural-elevation/#rendering
            var coast = r_elevation[r1] < 0 || r_elevation[r2] < 0;
            if (coast || s_flow[index] > 0 || s_flow[opposite_s] > 0)
            {
                // It's a coastal or river edge, forming a valley
                I[index * 3 + 2] = r1;
                I[index * 3 + 1] = numRegions + t2;
                I[index * 3 + 0] = numRegions + t1;
            }
            else
            {
                // It's a ridge
                I[index * 3 + 2] = r1;
                I[index * 3 + 1] = r2;
                I[index * 3 + 0] = numRegions + t1;
            }
        }
        private int s_inner_t(int s) { return s_to_t(s); }
        private int s_to_t(int s) { return s / 3; }
        private int s_begin_r(int s) { return _triangles[s]; }
        private int s_opposite_s(int s) { return _halfedges[s]; }
    }
}
