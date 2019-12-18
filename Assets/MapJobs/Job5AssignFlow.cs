using System;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Assets.MapJobs
{
    using Float = Double;

    unsafe struct Job5AssignFlow : IJob
    {
        public Float flow;

        public int numTriangles;

        [NativeDisableUnsafePtrRestriction] public int* _halfedges;

        [NativeDisableUnsafePtrRestriction] public Float* t_elevation;

        [NativeDisableUnsafePtrRestriction] public int* order_t;//缓存用的
        [NativeDisableUnsafePtrRestriction] public int* t_downslope_s;

        [NativeDisableUnsafePtrRestriction] public Float* t_moisture;
        [NativeDisableUnsafePtrRestriction] public Float* t_flow;
        [NativeDisableUnsafePtrRestriction] public Float* s_flow;

        public void Execute()
        {
            var flow = this.flow;
            var t_elevation = this.t_elevation;
            var s_flow = this.s_flow;
            var t_flow = this.t_flow;
            var t_moisture = this.t_moisture;

            //for (var i = 0; i < numTriangles * 3; i++) s_flow[i] = 0;
            Parallel.For(0, numTriangles * 3, i => s_flow[i] = 0);

            //for (var i = 0; i < numTriangles; i++)
            //{
            //    if (t_elevation[i] >= 0)
            //        t_flow[i] = flow * t_moisture[i] * t_moisture[i];
            //    else
            //        t_flow[i] = 0;
            //}
            Parallel.For(0, numTriangles, i =>
            {
                if (t_elevation[i] >= 0)
                    t_flow[i] = flow * t_moisture[i] * t_moisture[i];
                else
                    t_flow[i] = 0;
            });

            for (var i = order_t[0] - 1; i > 0; i--)
            {
                var t = order_t[i];
                var out_s = t_downslope_s[t];
                if (out_s >= 0)
                {
                    var out_t = s_outer_t(out_s);
                    t_flow[out_t] += t_flow[t];
                    s_flow[out_s] += t_flow[t]; // TODO: s_flow[t_downslope_s[t]] === t_flow[t]; redundant?
                    if (t_elevation[t] >= 0 && t_elevation[out_t] > t_elevation[t])
                    {
                        t_elevation[out_t] = t_elevation[t];
                    }
                }
            }
        }

        public int s_outer_t(int s) { return s_to_t(_halfedges[s]); }
        private int s_to_t(int s) { return s / 3; }
    }
}
