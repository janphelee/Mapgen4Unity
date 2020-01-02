using Unity.Collections;
using Unity.Jobs;

using Float = System.Single;
using Float2 = UnityEngine.Vector2;

namespace Assets.MapJobs
{

    struct Job4AssignAngleWind : IJobParallelFor
    {
        public Float2 windAngleVec;

        [ReadOnly] public NativeArray<Float2> r_vertex;

        [WriteOnly] public NativeArray<Float> r_wind_sort;

        public void Execute(int index)
        {
            r_wind_sort[index] = (vertex_x(index) * windAngleVec[0] + vertex_y(index) * windAngleVec[1]);
        }

        private Float vertex_x(int i) { return r_vertex[i][0]; }
        private Float vertex_y(int i) { return r_vertex[i][1]; }

    }
}
