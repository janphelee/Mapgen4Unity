using Unity.Collections;
using Unity.Jobs;

#if Use_Double_Float
using Float = System.Double;
using Float2 = Unity.Mathematics.double2;
#else
using Float = System.Single;
#endif

namespace Assets.MapJobs
{

    struct Job4AssignRainfall : IJob
    {
        public Float raininess;
        public Float rain_shadow;
        public Float evaporation;

        public int numBoundaryRegions;
        [ReadOnly] public NativeArray<int> _triangles;
        [ReadOnly] public NativeArray<int> _halfedges;
        [ReadOnly] public NativeArray<int> _r_in_s;
        [ReadOnly] public NativeArray<Float> r_elevation;

        [ReadOnly] public NativeArray<Float> r_wind_sort;
        [ReadOnly] public NativeArray<int> wind_order_r;

        [WriteOnly] public NativeArray<Float> r_rainfall;
        public NativeArray<Float> r_humidity;

        public void Execute()
        {
            for (int i = 0; i < wind_order_r.Length; ++i)
            {
                var r = wind_order_r[i];
                int count = 0;
                var sum = 0.0;
                int s0 = _r_in_s[r], incoming = s0;
                do
                {
                    var neighbor_r = s_begin_r(incoming);
                    if (r_wind_sort[neighbor_r] < r_wind_sort[r])
                    {
                        count++;
                        sum += r_humidity[neighbor_r];
                    }
                    var outgoing = s_next_s(incoming);
                    incoming = _halfedges[outgoing];
                } while (incoming != s0);

                double humidity = 0.0, rainfall = 0.0;
                if (count > 0)
                {
                    humidity = sum / count;
                    rainfall += raininess * humidity;
                }
                if (r_boundary(r))
                {
                    humidity = 1.0;
                }
                if (r_elevation[r] < 0.0)
                {
                    var evaporate = evaporation * -r_elevation[r];
                    humidity += evaporate;
                }
                if (humidity > 1.0 - r_elevation[r])
                {
                    var orographicRainfall = rain_shadow * (humidity - (1.0 - r_elevation[r]));
                    rainfall += raininess * orographicRainfall;
                    humidity -= orographicRainfall;
                }
                r_rainfall[r] = (float)rainfall;
                r_humidity[r] = (float)humidity;
            }
        }
        private bool r_boundary(int r) { return r < numBoundaryRegions; }
        private int s_begin_r(int s) { return _triangles[s]; }
        private int s_next_s(int s) { return (s % 3 == 2) ? s - 2 : s + 1; }
    }
}
