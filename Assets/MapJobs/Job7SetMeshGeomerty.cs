using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.MapJobs
{
    using Float2 = double2;

    unsafe struct Job7SetMeshGeomerty : IJobParallelFor
    {
        public int numRegions;

        [NativeDisableUnsafePtrRestriction] public Float2* _r_vertex;
        [NativeDisableUnsafePtrRestriction] public Float2* _t_vertex;

        [WriteOnly] public NativeArray<Vector3> vertex;

        public void Execute(int index)
        {
            vertex[index] = getV3(index);
        }

        private Vector3 getV3(int i)
        {
            if (i < numRegions)
            {
                var v = _r_vertex[i];
                return new Vector3((float)v.x, (float)v.y);
            }
            else
            {
                var v = _t_vertex[i - numRegions];
                return new Vector3((float)v.x, (float)v.y);
            }
        }
    }
}
