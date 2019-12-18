using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Assets.MapJobs
{
    unsafe struct Job5AssignDownslope : IJob
    {
        public int numTriangles;
        [NativeDisableUnsafePtrRestriction] public int* _halfedges;
        [NativeDisableUnsafePtrRestriction] public float* t_elevation;

        [NativeDisableUnsafePtrRestriction] public int* order_t;
        [NativeDisableUnsafePtrRestriction] public int* t_downslope_s;

        public void Execute()
        {

            //for (int i = 0; i < numTriangles; ++i) t_downslope_s[i] = -999;
            var t_downslope_s = this.t_downslope_s;
            Parallel.For(0, numTriangles, i => t_downslope_s[i] = -999);

            var queue = new FlatQueue(numTriangles);
            var count = 1;
            for (var t = 0; t < numTriangles; ++t)
            {
                if (t_elevation[t] < -0.1f)
                {
                    var best_s = -1;
                    var best_e = t_elevation[t];
                    for (var j = 0; j < 3; j++)
                    {
                        var s = 3 * t + j;
                        var e = t_elevation[s_outer_t(s)];
                        if (e < best_e)
                        {
                            best_e = e;
                            best_s = s;
                        }
                    }
                    order_t[count++] = t;
                    t_downslope_s[t] = best_s;
                    queue.push(t, t_elevation[t]);
                }
            }

            for (var i = 0; i < numTriangles; i++)
            {
                var t = queue.pop();
                for (var j = 0; j < 3; j++)
                {
                    var s = 3 * t + j;
                    var neighbor_t = s_outer_t(s);
                    if (t_downslope_s[neighbor_t] == -999)
                    {
                        t_downslope_s[neighbor_t] = s_opposite_s(s);
                        order_t[count++] = neighbor_t;
                        queue.push(neighbor_t, t_elevation[neighbor_t]);
                    }
                }
            }
            order_t[0] = count;

            queue.Dispose();
        }

        private int s_outer_t(int s) { return s_to_t(s_opposite_s(s)); }
        private int s_opposite_s(int s) { return _halfedges[s]; }
        private int s_to_t(int s) { return s / 3; }
    }
}
